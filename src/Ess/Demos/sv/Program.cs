using App.Sv;
using Machines;
using Sv;
using Sv.Host;

unsafe
{
    using var world = new FrameLoop<HostKernels>(PackData.Create(), new HostOptions { Workers = 1, EntityCapacity = 1024 });

    var hero = world.Spawn((int)Template.hero);
    var goblin = world.Spawn((int)Template.goblin);
    var dragon = world.Spawn((int)Template.dragon);

    var mood = new HeroSm();
    mood.Start();
    Show("start");

    world.Send(hero, goblin, (int)Kind.Goblin, (int)Channel.Physical, 30);
    world.Send(hero, dragon, (int)Kind.Dragon, (int)Channel.Fire, 45);
    StepAndFollow("goblin hits 30, dragon burns 45");

    world.Send(hero, hero, 0, (int)Channel.Healing, 50);
    StepAndFollow("hero heals 50");

    void StepAndFollow(string what)
    {
        world.Step();

        for (var at = 0; at < world.EventCount; at++)
        {
            var change = world.Events[at];
            if (change.Slot == hero && change.Event == (int)Event.LowHealth)
            {
                mood.DispatchEvent(change.Entered ? HeroSm.EventId.LOW_HEALTH : HeroSm.EventId.RECOVERED);
            }
        }

        Show(what);
    }

    void Show(string what)
    {
        var health = world.Read(hero, (int)Intrinsic.Health);
        var lowHealth = world.Level(hero, (int)Event.LowHealth);
        Console.WriteLine($"{what,-32} sv: health {health,3} low-health {lowHealth,-5}  StateSmith: {mood.stateId,-8} wounded {mood.vars.woundedCount} time(s)");
    }
}
