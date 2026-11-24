StateSmith and sv, in one ordinary .NET project.

  HeroSm.plantuml   the StateSmith diagram. You edit this. ess makes HeroSm.cs from it.
  lib/sv/           the sv engine, a git submodule. Never edit it.
  pack/             sv's data: values, rules, templates.
  Program.cs        the host. It is the only place that knows both.
  *.csproj          the project. It links lib/sv and compiles the made code.

StateSmith (HeroSm) does not know sv exists. sv does not know StateSmith exists.
The host asks sv for its events and sends them to the StateSmith machine.

Run it
  dotnet run -c Release

Change it
  ess w            make HeroSm.cs again every time you save HeroSm.plantuml
  pack/*.sv.json   change sv's numbers, run again

Get the latest sv
  ess u sv
