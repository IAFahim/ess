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

| `ess d`, `d1`, `d2` | demo | makes a demo folder to try (see below) |

Type `ess` alone and press one key. The menu stays open until you press `q`. If you mistype (`ess wach`, `ess gti`), `ess` asks what you meant.

## Try a demo

```
mkdir try
cd try
ess d
```

Each demo makes its own folder, makes the code, and (for `d` and `d1`) runs it with `dotnet run` so you see the result at once. It never overwrites a folder that is already there.

| Type | Folder | Shows |
|---|---|---|
| `ess d` | `demo-light` | a lamp: two states and a counter |
| `ess d1` | `demo-player` | nested states, a choice, history, and your own code beside the made struct |
| `ess d2` | `demo-unity-ecs` | Unity ECS: copy the folder into `Assets/`, run the EditMode tests, expect 3 passed |

Open the diagram in the demo folder, change it, and run `ess w` there.

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

## Unity ECS

`ess` always makes the machine a `partial struct`, never a class, so it is unmanaged data that Burst can run. It removes the two things C# 9 (Unity's language level) will not allow on a struct: the `= new Vars()` field initializer and the empty constructor.

To use it as a component, add this line to the `[RenderConfig.CSharp]` block of the diagram (the starter has it commented out):

```
BaseList = "Unity.Entities.IComponentData"
```

Rules for using it:

- Pass it by `ref`. A copy dispatches on the copy.
- A default (zero) struct has `stateId == StateId.ROOT`. Call `Start()` once when you see that.
- Actions in the diagram must be Burst safe: no `List`, no `string`, no managed objects. Put the data they need in `vars` (`AutoExpandedVars`).
- Talk to it through `vars`: set the inputs, `DispatchEvent`, read the outputs.
- `BaseList` may only name interfaces (a struct cannot have a base class). `NameSpace` must not end with `;`.

`scripts/unity-check.sh` runs the proof: a Burst `IJobEntity` drives the machine inside an ECS world and is compared with plain managed code.

If your diagram folder is inside a Unity project, the Unity project is already a git repo. `ess g` refuses it as "inside a bigger git project", and watch does not auto-save there. Use your project's own git for that folder.

## Two things in the diagram that surprise people

- `On --> On : EVENT / x` leaves and re-enters the state, so its `enter` action runs again. To stay in the state, write the event with no arrow: `On : EVENT / x`.
- A behavior with no arrow does not stop the event. The next line is checked too, with the changed values.

See [AGENTS.md](AGENTS.md) for how it works inside.
