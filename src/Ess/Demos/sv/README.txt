sv and StateSmith together.

  sv          keeps the numbers (health) and says when something happens (the low-health event).
  StateSmith  keeps the behaviour: Healthy, Wounded. HeroSm.plantuml is the diagram.
  Program.cs  is the glue. It asks sv for its events and sends them to the StateSmith struct.

Files
  sv/                the sv engine (a git clone). Never edit it.
  app/pack/          sv's data: values, rules, templates. Copied from sv's own sample.
  app/HeroSm.plantuml  the diagram. Edit this and run  ess w  in the app folder.
  app/HeroSm.cs      made by ess. A struct. Do not edit.
  app/Program.cs     the glue.

Run it
  dotnet run -c Release --project app

Try
  Open app/pack/rules.sv.json and change a number, run it again. sv's numbers change.
  Open app/HeroSm.plantuml and add a state, run  ess r  in the app folder, then use it in Program.cs.

Get the latest sv
  ess u sv
