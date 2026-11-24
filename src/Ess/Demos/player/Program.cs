using Machines;

var player = new PlayerSm();
player.Start();
Show("Start");

Send(PlayerSm.EventId.PLAY);

player.vars.trackLoaded = true;
Console.WriteLine("(a track is loaded now)");

Send(PlayerSm.EventId.PLAY);
Send(PlayerSm.EventId.PAUSE);
Send(PlayerSm.EventId.CALL);
Send(PlayerSm.EventId.HANGUP);
Send(PlayerSm.EventId.PAUSE);

for (var tick = 0; tick < 4; tick++)
{
    Send(PlayerSm.EventId.TICK);
}

void Send(PlayerSm.EventId eventId)
{
    player.DispatchEvent(eventId);
    Show(eventId.ToString());
}

void Show(string what)
{
    var vars = player.vars;
    Console.WriteLine($"{what,-7} -> {player.stateId,-12} position={vars.position} play={vars.playCount} pause={vars.pauseCount} leave={vars.leaveCount}");
}
