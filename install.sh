#!/bin/sh
set -eu

repo="IAFahim/ess"
base_url="${ESS_BASE_URL:-https://github.com/$repo/releases/latest/download}"
install_dir="${ESS_INSTALL_DIR:-$HOME/.local/bin}"

case "$(uname -s)-$(uname -m)" in
  Linux-x86_64)               asset="ess-linux-x64.tar.gz" ;;
  Linux-aarch64|Linux-arm64)  asset="ess-linux-arm64.tar.gz" ;;
  Darwin-arm64)               asset="ess-osx-arm64.tar.gz" ;;
  Darwin-x86_64)              asset="ess-osx-x64.tar.gz" ;;
  *) echo "ess has no download for $(uname -s) $(uname -m)" >&2; exit 1 ;;
esac

work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

if ! curl -fsSL "$base_url/$asset" -o "$work_dir/$asset"; then
  if command -v gh >/dev/null 2>&1; then
    gh release download --repo "$repo" --pattern "$asset" --dir "$work_dir"
  else
    echo "Could not download $asset. If the repo is private, install gh and run: gh auth login" >&2
    exit 1
  fi
fi

tar -xzf "$work_dir/$asset" -C "$work_dir"
mkdir -p "$install_dir"
install -m 755 "$work_dir/ess" "$install_dir/ess"

echo "Installed ess to $install_dir/ess"
case ":$PATH:" in
  *":$install_dir:"*) ;;
  *) echo "Add this folder to your PATH:  export PATH=\"$install_dir:\$PATH\"" ;;
esac
"$install_dir/ess" --version
