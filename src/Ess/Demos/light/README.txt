A lamp. Two states: Off and On.

  TOGGLE   switches it on and off.
  TICK     makes it brighter, up to level 3.

Files
  LightSm.plantuml   the diagram. This is the only file you edit.
  LightSm.cs         made by ess. A struct. Do not edit.
  LightSm.md         made by ess. The design as text.
  LightSm.sim.html   made by ess. Open it in a browser and click the events.
  Program.cs         sends events to the lamp and prints what happens.

Run it
  dotnet run

Change it
  ess w
  Open LightSm.plantuml, change  [level < 3]  to  [level < 5], save.
  Then:  dotnet run
