using Machines;

var lamp = new LightSm();
lamp.Start();
Show("Start");

lamp.DispatchEvent(LightSm.EventId.TOGGLE);
Show("TOGGLE");

for (var tick = 0; tick < 4; tick++)
{
    lamp.DispatchEvent(LightSm.EventId.TICK);
    Show("TICK");
}

lamp.DispatchEvent(LightSm.EventId.TOGGLE);
Show("TOGGLE");

void Show(string eventName)
{
    Console.WriteLine($"{eventName,-8} -> {lamp.stateId,-4} level={lamp.vars.level}");
}
