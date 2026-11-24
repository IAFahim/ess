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

| Type | Name | Does |
|---|---|---|
| `ess` | menu | one key menu; stays open until q |
| `ess n [Name]` (`in`) | new | starter diagram, get StateSmith, make the code |
| `ess r` | run | make the code once |
| `ess w` | watch | make the code on every save; commit and push every good build if the folder is a git repo; key `g` saves now, `q` stops |
| `ess g` (`s`) | git | git init if needed, commit, create a private GitHub repo if none, push |

Typos are forgiven: capitals, a unique prefix (`wa`), long names (`watch`), and swapped letters (`wach`, `gti`) which ask "did you mean". Anything else shows the menu.

## Layers

| Layer | One job | Input -> output | Law, tested alone |
|---|---|---|---|
| `CommandResolver` | typed text to a command | `string` -> `CommandResolution` | `CommandResolverTests` |
| `EditDistance` | how far apart two words are | two `string` -> `int` | swap counts as 1 |
| `StructConverter` | StateSmith's class into a C# 9 safe partial struct | generated `.cs` text -> `StructConversion` | compiled with Roslyn at C# 9, `IsUnmanagedType`, same trace as the class; real fixtures in `Fixtures/generated` |
| `GenerationOutputJudge` | did StateSmith work, which files, what went wrong | `ProcessResult` -> bool, files, text | real recorded outputs in `tests/Ess.Tests/Fixtures` |
| `StateSmithAssets` | which download for which computer | `HostPlatform` -> asset + SHA-256 | `StateSmithAssetsTests` |
| `MachineNames`, `RepoNames` | folder or typed text to a legal name | `string` -> `string` | `NamesTests` |

The rest (`StateSmithTool`, `CodeGenerator`, `GitSaver`, `Subprocess`, the commands) are I/O shells. They have no unit tests; `scripts/smoke.sh <ess binary>` runs the whole flow against a local bare git remote.

## Hard rules

- No code comments. Names say the role.
- Zero warnings. `TreatWarningsAsErrors` is on, which also makes NuGet audit fail the build. Build with an SDK whose bundled runtime is 10.0.14 or newer: 10.0.12 links a Brotli decoder with a known high severity bug (GHSA-p3jp-9v83-76w9) into Native AOT binaries.
- No mutable statics.
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
