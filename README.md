# ess - easy StateSmith

Short commands for [StateSmith](https://github.com/StateSmith/StateSmith). Write a diagram as text. Get C# code. Save it to GitHub.

You do not need an IDE. You do not need to remember long commands.

## The four keys

| Type | Means | Does |
|---|---|---|
| `ess n` | new | makes a starter diagram, gets StateSmith, makes the code |
| `ess r` | run | makes the code one time |
| `ess w` | watch | makes the code every time you save |
| `ess g` | git | saves to git and pushes to GitHub |

Type `ess` alone and press one key. If you mistype, `ess` asks what you meant.

## First time

```
mkdir door
cd door
ess n
ess w
```

Open `DoorSm.plantuml` in any text editor. Save it. The code is made again. If the folder is already a git project (`ess g` once), every good save is committed and pushed. A broken diagram is never saved.

## Install

Linux and macOS:

```
curl -fsSL https://raw.githubusercontent.com/IAFahim/ess/main/install.sh | sh
```

Windows (PowerShell):

```
irm https://raw.githubusercontent.com/IAFahim/ess/main/install.ps1 | iex
```

These only work after a release exists, and the repo is private until you decide otherwise (a private repo needs `gh auth login` first). Build it yourself meanwhile: `dotnet publish src/Ess -c Release -r linux-x64 -o out`.

`ess g` needs `git`. To put the folder on GitHub it also needs [`gh`](https://cli.github.com), logged in once with `gh auth login`.

## Two things in the diagram that surprise people

- `On --> On : EVENT / x` leaves and re-enters the state, so its `enter` action runs again. To stay in the state, write the event with no arrow: `On : EVENT / x`.
- A behavior with no arrow does not stop the event. The next line is checked too, with the changed values.

See [AGENTS.md](AGENTS.md) for how it works inside.
