# ess - easy StateSmith

Short commands for [StateSmith](https://github.com/StateSmith/StateSmith). Write a diagram as text. Get C# code. Save it to GitHub.

You do not need an IDE. You do not need to remember long commands.

## Commands

`ess <command> [arguments] [options]`. Every command has a short form. `ess --help` lists everything, `ess <command> --help` explains one.

| Type | Short | Does |
|---|---|---|
| `ess new [what]` | `n`, `in` | makes a starter diagram here. `ess n sv` makes a project linking StateSmith and sv |
| `ess unity [what]` | `p` | makes this folder a Unity package with a starter machine. In Unity: Package Manager, +, Install package from disk, pick its `package.json`. The diagrams stay here, outside Unity. The machine is plain C#, so `ess r` runs it in .NET too |
| `ess run` | `r` | makes the code once. In a folder made by `ess unity`, it also runs the machine in .NET |
| `ess watch` | `w` | makes the code every time you save. While watching: `g` saves to GitHub now, `q` stops |
| `ess git` | `g`, `s` | saves to git and pushes to GitHub |
| `ess demo [which]` | `d` | lists the demos. `ess d 1` or `ess d light` makes one |
| `ess update <what>` | `u` | gets the latest of something: `ess u sv` |
| `ess open [file]` | `o` | Linux: gets or updates [omaspec](https://github.com/enhapu-afk/omaspec), makes it the app that opens `.plantuml` files, then opens the diagrams here |

`ess` alone in a terminal shows a one-key menu. If you mistype a command, `ess` tells you what you probably meant. `ess --version` shows the version.

## Use it in Unity

Keep the diagrams outside Unity. Unity only gets the code.

```
mkdir my-machines
cd my-machines
ess unity
```

Then, in Unity (once): Window > Package Management > Package Manager, press `+`, **Install package from disk...**, and pick `my-machines/package.json`.

Now run `ess w` in `my-machines`. Every time you save a diagram, Unity gets the new struct. Each machine is an ECS component in the namespace `Machines` and runs in Burst jobs.

You do not need Unity to try a machine:

```
ess r
```

```
Start    -> OFF  count=0
TOGGLE   -> ON   count=1
TOGGLE   -> OFF  count=0
TOGGLE   -> ON   count=1
```

What is in the folder:

| File | What it is |
|---|---|
| `MyMachinesSm.plantuml` | the diagram. You edit this |
| `MyMachinesSm.cs` | the machine, plain C#. `ess` makes it |
| `MyMachinesSm.sim.html` | a simulator. Open it in a browser and click the events |
| `Unity/MyMachinesSm.Component.cs` | one line that makes the machine an ECS component. Only Unity reads it |
| `Dotnet~/` | a small .NET program that runs the machine. Unity skips folders ending in `~` |
| `package.json`, `Ess.MyMachines.asmdef` | what Unity needs to load the package |

## Try a demo

```
ess d
```

lists the demos. Make one with its number or its name, from any folder:

```
ess d 1
ess d sv
```

Each demo makes its own folder right there, makes the code, and (for 1, 2 and 4) runs it so you see the result at once. If your `dotnet` on the PATH has no SDK, `ess` looks for a .NET 10 SDK itself (`DOTNET_ROOT`, `~/.dotnet`, the usual system folders, mise) and uses that. It never overwrites a folder that is already there, except `ess d sv`, which resets its own `demo-sv` on purpose.

| Demo | Folder | Shows |
|---|---|---|
| `1`, `light` | `demo-light` | a lamp: two states and a counter |
| `2`, `player` | `demo-player` | nested states, a choice, history, and your own code beside the made struct |
| `3`, `ecs` | `demo-unity-ecs` | Unity ECS: copy the folder into `Assets/`, run the EditMode tests, expect 3 passed |
| `4`, `sv` | `demo-sv` | a .NET project linking a StateSmith machine and sv (in `lib/sv`). Neither knows the other |

Open the diagram in the demo folder, change it, and run `ess w` there.

## sv

[sv](https://github.com/IAFahim/sv) is a public repo, so these need only `git` and a connection. No login.

StateSmith and sv are two libraries that do not know each other. One ordinary .NET project links them. `ess` makes that project:

```
demo-sv/
  DemoSv.csproj       the project. Run it with:  dotnet run -c Release
  HeroSm.plantuml     the StateSmith diagram (ess makes HeroSm.cs from it)
  Program.cs          the host. The only code that knows both
  pack/               sv's data: values, rules, templates
  lib/sv/             sv itself, a git submodule. Never edit it
```

- `ess d sv` makes `demo-sv` and runs it. sv keeps the hero's health and fires its low-health event. The host sends that event to the StateSmith struct (`HeroSm`): Healthy, Wounded, Healthy. Run it again and it gets the latest sv and resets the whole folder, throwing away any changes you made inside it.
- `ess n sv` makes the same project in the current folder (it starts a git repo there if there is none). It never touches a folder that already has a project or sv.
- `ess u sv` brings `lib/sv` up to the latest (a fast-forward). It stops and tells you if you changed something in sv.

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

On Linux the installer also runs `ess o` once, so double-clicking a `.plantuml` or `.puml` file opens [omaspec](https://github.com/enhapu-afk/omaspec), which draws the diagram next to its text. Run `ess o` again any time to update omaspec. On Windows and macOS, open the `.sim.html` file next to a diagram in a browser instead.

## First time

```
mkdir door
cd door
ess n
ess w
```

Open `DoorSm.plantuml` in any text editor and save it (on Linux, `ess o` opens it in omaspec, which shows the drawing as you type). The code is made again. If the folder is a git project (press `g` once), every good save is committed and pushed. A broken diagram is never saved.

## Unity ECS

`ess` always makes the machine a `partial struct`, never a class, so it is unmanaged data that Burst can run. It removes the two things C# 9 (Unity's language level) will not allow on a struct: the `= new Vars()` field initializer and the empty constructor.

In a folder made by `ess unity` this is done for you: the machine stays plain C# and `Unity/<Name>.Component.cs` makes it a component. Anywhere else, to use it as a component, add this line to the `[RenderConfig.CSharp]` block of the diagram (the starter has it commented out):

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
