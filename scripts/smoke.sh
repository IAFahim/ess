#!/bin/sh
set -eu

ess_binary="${1:?usage: scripts/smoke.sh /path/to/ess}"
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

export ESS_HOME="$work_dir/home"
export GIT_CONFIG_GLOBAL="$work_dir/gitconfig"
export GIT_CONFIG_SYSTEM=/dev/null
printf '[user]\n\tname = smoke\n\temail = smoke@example.invalid\n' > "$GIT_CONFIG_GLOBAL"

project="$work_dir/door-lamp"
remote="$work_dir/remote.git"
mkdir -p "$project"
git init --bare -q "$remote"
cd "$project"

expect_in() {
  case "$2" in *"$1"*) echo "pass: $3" ;; *) echo "FAIL: $3 (wanted '$1')"; echo "$2"; exit 1 ;; esac
}

expect_in "ess 0." "$("$ess_binary" --version)" "version"
expect_in "Usage: ess" "$(echo q | "$ess_binary")" "no arguments prints help"
expect_in "Usage: ess demo" "$("$ess_binary" d --help)" "help works after a command"
expect_in "ess d <number or name>" "$("$ess_binary" d)" "d lists the demos"
expect_in "Did you mean:  ess watch" "$("$ess_binary" wach 2>&1 || true)" "a typo gets a suggestion"
expect_in "Did you mean:  ess d sv" "$("$ess_binary" dsv 2>&1 || true)" "an old form points at the new one"
expect_in "Unknown option" "$("$ess_binary" r --fast 2>&1 || true)" "an unknown option is an error"
expect_in "There is no demo" "$("$ess_binary" d 9 2>&1 || true)" "an unknown demo is an error"
expect_in "no diagram" "$("$ess_binary" r || true)" "run without a diagram says so"

expect_in "made DoorLampSm.cs" "$("$ess_binary" n)" "new downloads StateSmith and makes code"
test -f DoorLampSm.plantuml && test -f DoorLampSm.cs && test -f DoorLampSm.sim.html && echo "pass: files exist"

cp DoorLampSm.plantuml good.txt
sed -i.bak 's/\[\*\] --> Off/[*] --> Off #brown/' DoorLampSm.plantuml
expect_in "no viable alternative" "$("$ess_binary" r || true)" "broken diagram explains the problem"
cp good.txt DoorLampSm.plantuml
rm -f good.txt DoorLampSm.plantuml.bak

git init -q -b main
git remote add origin "$remote"
expect_in "Pushed to GitHub" "$("$ess_binary" g)" "g pushes to the remote"
expect_in "Nothing new" "$("$ess_binary" g)" "g again has nothing new"

git -C "$remote" log --oneline main | grep -q "ess: save" && echo "pass: remote has the commit"
demo_dir="$work_dir/demos"
mkdir -p "$demo_dir"
cd "$demo_dir"
if dotnet --version >/dev/null 2>&1; then
  expect_in "TOGGLE   -> OFF  level=0" "$("$ess_binary" d 1)" "demo 1 runs"
  expect_in "HANGUP  -> PAUSED" "$("$ess_binary" d player)" "demo player runs and history resumes"
else
  echo "skip: dotnet not found, demos 1 and player not run"
fi
expect_in "Expect 3 passed" "$("$ess_binary" d 3)" "demo 3 (ecs) is made"
expect_in "already here" "$("$ess_binary" d 3 || true)" "a demo never overwrites"

package_dir="$work_dir/my-machines"
mkdir -p "$package_dir"
cd "$package_dir"
expect_in "made Unity/MyMachinesSm.Component.cs" "$("$ess_binary" unity)" "unity makes a package and a component file"
if grep -q "IComponentData" MyMachinesSm.cs; then
  echo "FAIL: the machine itself has Unity in it"
  exit 1
fi
echo "pass: the machine itself has no Unity in it"
if dotnet --version >/dev/null 2>&1; then
  expect_in "TOGGLE   -> ON   count=1" "$("$ess_binary" r)" "r runs the package machine in .NET, no Unity"
else
  echo "skip: dotnet not found, the package machine was not run"
fi
package_remote="$work_dir/package-remote.git"
git init --bare -q "$package_remote"
git init -q -b main
git remote add origin "$package_remote"
expect_in "Pushed to GitHub" "$("$ess_binary" g)" "g pushes the package"
saved="$(git ls-tree -r --name-only HEAD)"
expect_in "Dotnet~/Program.cs" "$saved" "git keeps Dotnet~"
expect_in "Unity/MyMachinesSm.Component.cs" "$saved" "git keeps the Unity folder"
case "$saved" in *"/bin/"*|*"/obj/"*) echo "FAIL: git keeps the .NET build output"; exit 1 ;; esac
echo "pass: git does not keep the .NET build output"

echo "all smoke checks passed"
