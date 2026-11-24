#!/bin/sh
set -eu

ess_binary="${1:?usage: scripts/unity-check.sh /path/to/ess /path/to/unity/project}"
unity_project="${2:?usage: scripts/unity-check.sh /path/to/ess /path/to/unity/project}"
repo_dir="$(cd "$(dirname "$0")/.." && pwd)"
hub_dir="${UNITY_HUB_EDITORS:-$HOME/Unity/Hub/Editor}"
unity="${UNITY:-$hub_dir/$(ls -1 "$hub_dir" | sort -V | tail -n 1)/Editor/Unity}"
out_dir="${OUT:-$(mktemp -d)}"
probe_dir="$unity_project/Assets/EssStructProbe"

work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

cp "$repo_dir/unity/PlayerSm.plantuml" "$work_dir/"
(cd "$work_dir" && "$ess_binary" r)

mkdir -p "$probe_dir"
cp "$repo_dir"/unity/EssStructProbe/* "$probe_dir/"
cp "$work_dir/PlayerSm.cs" "$probe_dir/"
grep -q "public partial struct PlayerSm" "$probe_dir/PlayerSm.cs"

"$unity" -batchmode -nographics -projectPath "$unity_project" -runTests -testPlatform EditMode \
  -testResults "$out_dir/results.xml" -logFile "$out_dir/unity.log" || true

rm -r "$probe_dir" "$probe_dir.meta"

echo "errors CS:   $(grep -c 'error CS' "$out_dir/unity.log" || true)"
echo "errors BC:   $(grep -c 'error BC' "$out_dir/unity.log" || true)"
echo "own warnings: $(grep -h 'warning [A-Z]*[0-9]*' "$out_dir/unity.log" | grep -c 'Assets/EssStructProbe' || true)"
grep -o '<test-run [^>]*' "$out_dir/results.xml" | grep -oE '(total|passed|failed|skipped)="[0-9]+"' | tr '\n' ' '
echo
echo "log: $out_dir/unity.log"
