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
expect_in "Press one key" "$(echo q | "$ess_binary")" "menu"
expect_in "Did you mean" "$(echo n | "$ess_binary" wach || true)" "typo asks did-you-mean"
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
echo "all smoke checks passed"
