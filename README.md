# ess - easy StateSmith

Short commands for [StateSmith](https://github.com/StateSmith/StateSmith). Write a diagram as text. Get C# code. Save it to GitHub.

You do not need an IDE. You do not need to remember long commands.

## The four keys

| Type | Means | Does |
|---|---|---|
| `ess n` (or `in`) | new | makes a starter diagram, gets StateSmith, makes the code |
| `ess r` | run | makes the code one time |
| `ess w` | watch | makes the code every time you save. Press `g` to save to GitHub now, `q` to stop |
| `ess g` (or `s`) | git, save | saves to git and pushes to GitHub |

Type `ess` alone and press one key. The menu stays open until you press `q`. If you mistype (`ess wach`, `ess gti`), `ess` asks what you meant.

## Install: one line

Pick the line for your computer. It installs `ess`, checks that `git` and `gh` are ready, and tells you what to do next.

Linux and macOS, while this repo is private (needs `gh` logged in):

```
gh api repos/IAFahim/ess/contents/install.sh -H 'Accept: application/vnd.github.raw' | sh
```

Windows PowerShell, while this repo is private (needs `gh` logged in):

```
gh api repos/IAFahim/ess/contents/install.ps1 -H 'Accept: application/vnd.github.raw' | iex
```

If the repo is public, no `gh` is needed to install:

```
curl -fsSL https://raw.githubusercontent.com/IAFahim/ess/main/install.sh | sh
```

```
irm https://raw.githubusercontent.com/IAFahim/ess/main/install.ps1 | iex
```

These download a release. They work only after a release exists (`git tag v0.1.0 && git push origin v0.1.0` builds all six binaries). Until then, build it yourself with an SDK that bundles .NET runtime 10.0.14 or newer (older ones stop with NU1903): `dotnet publish src/Ess -c Release -r linux-x64 -o out`.

`ess g` needs `git`. To put a folder on GitHub it also needs [`gh`](https://cli.github.com), logged in once with `gh auth login`.

## First time

```
mkdir door
cd door
ess n
ess w
```

Open `DoorSm.plantuml` in any text editor and save it. The code is made again. If the folder is a git project (press `g` once), every good save is committed and pushed. A broken diagram is never saved.

## Two things in the diagram that surprise people

- `On --> On : EVENT / x` leaves and re-enters the state, so its `enter` action runs again. To stay in the state, write the event with no arrow: `On : EVENT / x`.
- A behavior with no arrow does not stop the event. The next line is checked too, with the changed values.

See [AGENTS.md](AGENTS.md) for how it works inside.
