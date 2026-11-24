# ess - easy StateSmith

## Start here

This work ends in Unity ECS, so before you change the generated code or run Unity: `git -C /home/os/GitHub/unity-cli-skill pull --ff-only`, then read `/home/os/GitHub/unity-cli-skill/skills/unity-cli-field-notes/SKILL.md` top to bottom. The rules that bite here: ISystem, IJobEntity, Burst and unmanaged data only (9); Unity 7 compiles C# 9 and zero warnings from your own files is the bar (22); one Unity process at a time through `_work/unity.lock` (6); never hand-write `.meta` or other serialized files (8); delete scratch projects you made (Done means); push what you learned back (1).

## What it is

`ess` is a small command line tool for one person who is dyslexic and cannot hold long commands in their head. It turns a folder of PlantUML text diagrams into generated C# state machines, and saves the result to GitHub, using one letter commands and a one key menu.

It wraps StateSmith's own `ss.cli` (pinned, downloaded on first use, SHA-256 checked) and the `git` and `gh` programs. It does not reimplement either.

It makes StateSmith's C# class a `partial struct` so the machine is unmanaged and can be an ECS component under Burst.

It builds one Native AOT binary, `ess` (about 7 MB, no .NET needed to run it).

## Shape of the data

- Input: `*.plantuml`, `*.puml`, `*.pu` text files in one folder. Each carries its own settings in a `/'! $CONFIG : toml ... '/` block.
- Output: files StateSmith writes next to each diagram (`<Name>.cs`, `<Name>.md`, `<Name>.sim.html`).
- State: nothing in the project. The pinned tool lives in `~/.ess/statesmith-<version>/` (`ESS_HOME` overrides the folder).

## Who uses it, how

| Type | Short | Does |
|---|---|---|
| `ess` | | in a terminal a one key menu (stays open until q); otherwise the help |
| `ess new [what]` | `n`, `in` | starter diagram, get StateSmith, make the code. `ess n sv`: `./sv` (clone) and `./app` (sv + StateSmith project); refuses if either exists. `sv` is reserved |
| `ess run` | `r` | make the code once |
| `ess watch` | `w` | make the code on every save; commit and push every good build if the folder is a git repo; key `g` saves now, `q` stops |
| `ess git` | `g`, `s` | git init if needed, commit, create a private GitHub repo if none, push |
| `ess demo [which]` | `d` | no argument: list the demos numbered from 1. A number or a name makes `demo-light` (1), `demo-player` (2), `demo-unity-ecs` (3), `demo-sv` (4) in the current folder; never overwrites a folder. `ess d sv` resets its own marked `demo-sv`: fetch, `reset --hard @{upstream}`, `clean -fd` in `sv/`, rewrite `app/` (only when `origin` is the sv URL) |
| `ess update <what>` | `u` | `ess u sv`: `git pull --ff-only` in `./sv` (refuse and say so if it has local changes) |

The grammar copies ConsoleAppFramework's rules, written by hand: a verb with aliases, then positional arguments, then options; `-h`/`--help` anywhere and on every command; `--version`; an unknown command or option is an error on stderr with exit code 1; `--` ends options. The library itself was not adopted because its `--help` needs XML doc comments and this repo has no code comments. `CommandTable` is the single source for the parser and the help, and `ArgumentParserTests` parses every example the help prints. There is no prefix matching and no bare-word commands: a typo gets a suggestion (a y/n prompt only when stdin is a terminal), never a guess.

## Layers

| Layer | One job | Input -> output | Law, tested alone |
|---|---|---|---|
| `ArgumentParser`, `CommandTable`, `HelpWriter` | `string[]` to an `Invocation`; the one table of commands; help text from it | `string[]` -> `Invocation`; `CommandSpec` -> lines | `ArgumentParserTests`, `HelpWriterTests` |
| `EditDistance` | how far apart two words are | two `string` -> `int` | swap counts as 1 |
| `DemoSelector`, `DemoCatalog` | number or name to a demo; which demos exist | `string` -> `DemoInfo?` | `DemoTests`; the demo files are embedded from `src/Ess/Demos` and `unity/` (one source for the ECS demo and its Unity probe) |
| `SvRepo` | clone, recognise and update one git clone | folder + source URL -> bool, printed result | `SvRepoTests` against a local bare repo (clone, already latest, pull, refuse dirty, reset) |
| `StructConverter` | StateSmith's class into a C# 9 safe partial struct | generated `.cs` text -> `StructConversion` | compiled with Roslyn at C# 9, `IsUnmanagedType`, same trace as the class; real fixtures in `Fixtures/generated` |
| `GenerationOutputJudge` | did StateSmith work, which files, what went wrong | `ProcessResult` -> bool, files, text | real recorded outputs in `tests/Ess.Tests/Fixtures` |
| `StateSmithAssets` | which download for which computer | `HostPlatform` -> asset + SHA-256 | `StateSmithAssetsTests` |
| `MachineNames`, `RepoNames` | folder or typed text to a legal name | `string` -> `string` | `NamesTests` |

The rest (`StateSmithTool`, `CodeGenerator`, `GitSaver`, `Subprocess`, the commands) are I/O shells. They have no unit tests; `scripts/smoke.sh <ess binary>` runs the whole flow against a local bare git remote.

## Hard rules

- No code comments. Names say the role.
- Zero warnings. `TreatWarningsAsErrors` is on, which also makes NuGet audit fail the build. Build with an SDK whose bundled runtime is 10.0.14 or newer: 10.0.12 links a Brotli decoder with a known high severity bug (GHSA-p3jp-9v83-76w9) into Native AOT binaries.
- No mutable statics.
- sv (`IAFahim/sv`, public since 2026-11-24) is only ever cloned and read from `ess`: never edit, commit or push inside a clone. `ess d sv` resets only a `demo-sv` folder that holds the `.ess-demo` marker file ess wrote; anything else is refused. `sv/` inside it is reset only when its `origin` equals the sv URL. The sv URL lives in `Program.cs` only. `scripts/smoke.sh` does not run the sv commands because they need network; they were run by hand against the real repo.
- The machine is always a `partial struct`. The converter refuses (and nothing is committed) when the class line is not found exactly once, the constructor has code, or `NameSpace` ends with `;` (file-scoped namespaces are C# 10). A struct field cannot have an initializer and a struct cannot have a parameterless constructor in C# 9, which is why both are removed. Never enable the `outputEventIdToStringFunction` settings: they return strings, which Burst cannot run.
- One place for the StateSmith version: `StateSmithAssets.cs`. To bump it, read the new digests with `gh api repos/StateSmith/StateSmith/releases/tags/v<version> --jq '.assets[] | [.name, .digest] | @tsv'`, then re-record both fixtures in `tests/Ess.Tests/Fixtures` from the new `ss.cli` and rerun the tests.
- `ss.cli run` exits 0 even when a diagram fails to parse (its `Program.cs` overwrites the exit code that `RunHandler` sets, see the TODO there). Success is judged from the output text ("Finished with failure"), never from the exit code alone. A broken diagram must never be committed or pushed.
- `ess g` never touches a folder that is inside a bigger git project, never creates a public repo, and uses the GitHub noreply identity for that folder only when git has no identity.

## Verify

```
dotnet test tests/Ess.Tests
dotnet publish src/Ess -c Release -r <rid> -o out
scripts/smoke.sh out/ess
python3 scripts/pty_smoke.py out/ess
scripts/unity-check.sh out/ess <scratch unity project>
```

`pty_smoke.py` drives the menu and the watch keys in a real terminal (Linux and macOS only).

Verified on Linux x64 only, including Unity 7000.0.0a20 with Entities 7.0.0 and Burst 2.0.0 (`scripts/unity-check.sh`: a `[BurstCompile]` `IJobEntity` drives the struct as an `IComponentData` through choice and history, equal to managed code step for step; a job without `[BurstCompile]` flips the witness, so the witness is not vacuous). macOS, Windows and ARM are unverified: see the open issues.
