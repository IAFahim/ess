A music player. It shows four things the lamp does not.

  Nested states   Playing and Paused live inside Active.
  A choice        PLAY only starts if a track is loaded, else it stays Stopped.
  History         CALL interrupts. HANGUP goes back to where you were (Paused).
  Your own code   The diagram calls OnPlay(), OnPause(), OnLeave().
                  You write them in PlayerSm.Actions.cs, next to the made code.

Files
  PlayerSm.plantuml     the diagram.
  PlayerSm.Actions.cs   your code. It is a partial struct, so it joins PlayerSm.cs.
  PlayerSm.cs           made by ess. Do not edit.
  Program.cs            sends events and prints what happens.

Run it
  dotnet run

Look closely
  The 3rd TICK goes to Stopped, not Playing with position 3.
  A line with no arrow (TICK [position < 3] / position += 1;) does not stop the event.
  The next line (TICK [position >= 3]) is checked too, with the new value.
  Try writing the second guard as  [position > 3]  and run it again.
