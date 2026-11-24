import os, pty, sys, time, select, subprocess, tempfile, shutil

ess = sys.argv[1]
work = tempfile.mkdtemp()
home = os.path.join(work, "home")
project = os.path.join(work, "door"); os.makedirs(project)
remote = os.path.join(work, "remote.git")
subprocess.run(["git", "init", "--bare", "-q", remote], check=True)
gitconfig = os.path.join(work, "gitconfig")
open(gitconfig, "w").write("[user]\n\tname = pty\n\temail = pty@example.invalid\n")
env = dict(os.environ, ESS_HOME=home, GIT_CONFIG_GLOBAL=gitconfig, GIT_CONFIG_SYSTEM="/dev/null", TERM="xterm")

pid, fd = pty.fork()
if pid == 0:
    os.chdir(project)
    os.execve(ess, [ess], env)

captured = b""
def pump(seconds):
    global captured
    end = time.time() + seconds
    while time.time() < end:
        ready, _, _ = select.select([fd], [], [], 0.2)
        if ready:
            try:
                data = os.read(fd, 65536)
            except OSError:
                return False
            if not data:
                return False
            captured += data
    return True

def press(key, wait):
    os.write(fd, key.encode())
    pump(wait)

pump(1.5)
press("n", 25)
subprocess.run(["git", "init", "-q", "-b", "main"], cwd=project, check=True)
subprocess.run(["git", "remote", "add", "origin", remote], cwd=project, check=True)
press("w", 4)
with open(os.path.join(project, "DoorSm.plantuml"), "a") as f:
    f.write("\n' touched\n")
pump(6)
press("g", 5)
press("q", 2)
press("r", 4)
press("x", 1)
press("q", 2)
try:
    os.waitpid(pid, 0)
except ChildProcessError:
    pass

text = captured.decode(errors="replace")
log = subprocess.run(["git", "-C", remote, "log", "--oneline", "main"], capture_output=True, text=True).stdout
print(text)
print("=== remote log\n" + log)
shutil.rmtree(work, ignore_errors=True)
assert "Stopped watching." in text and "is not one of the keys" in text, "menu or watch keys did not behave"
assert "ess: DoorSm.plantuml" in log, "watch did not push the edit"
print("pty smoke passed")
