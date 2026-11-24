using Xunit;

namespace Ess.Tests;

public class StructBehavesLikeClassTests
{
    private readonly StructConverter converter = new();
    private readonly GeneratedFixtures fixtures = new();
    private readonly CSharp9Compilation compilation = new();

    private IReadOnlyList<string> Trace(string fixtureName, string typeName, bool asStruct, Action<MachineDriver, List<string>> script)
    {
        var text = fixtures.Text(fixtureName);
        var source = asStruct ? converter.Convert(text).Text : text;
        var compiled = compilation.Compile(source, typeName, fixtureName + (asStruct ? "S" : "C") + Guid.NewGuid().ToString("N"));
        Assert.NotNull(compiled.Assembly);

        var driver = new MachineDriver(compiled.Assembly, typeName);
        var trace = new List<string>();
        script(driver, trace);
        return trace;
    }

    private static void Step(MachineDriver driver, List<string> trace, string eventName)
    {
        driver.Dispatch(eventName);
        trace.Add($"{eventName} -> {driver.Snapshot()}");
    }

    [Fact]
    public void PlayerWithChoiceAndHistoryGivesTheSameTraceAsAStructAndAsAClass()
    {
        void Script(MachineDriver driver, List<string> trace)
        {
            driver.Start();
            trace.Add($"start -> {driver.Snapshot()}");
            Step(driver, trace, "PLAY");
            driver.SetVar("trackLoaded", true);
            Step(driver, trace, "PLAY");
            Step(driver, trace, "PAUSE");
            Step(driver, trace, "CALL");
            Step(driver, trace, "HANGUP");
            Step(driver, trace, "PAUSE");
            Step(driver, trace, "TICK");
            Step(driver, trace, "TICK");
            Step(driver, trace, "TICK");
            Step(driver, trace, "TICK");
            Step(driver, trace, "PLAY");
            Step(driver, trace, "STOP");
        }

        var asClass = Trace("PlayerSm", "Machines.PlayerSm", asStruct: false, Script);
        var asStruct = Trace("PlayerSm", "Machines.PlayerSm", asStruct: true, Script);

        Assert.Equal(asClass, asStruct);
        Assert.Contains(asStruct, line => line.StartsWith("HANGUP -> PAUSED", StringComparison.Ordinal));
        Assert.Contains(asStruct, line => line.StartsWith("PLAY -> STOPPED", StringComparison.Ordinal));
        Assert.Contains(asStruct, line => line.StartsWith("PLAY -> PLAYING", StringComparison.Ordinal));
    }

    [Fact]
    public void StarterGivesTheSameTraceAsAStructAndAsAClass()
    {
        void Script(MachineDriver driver, List<string> trace)
        {
            driver.Start();
            trace.Add($"start -> {driver.Snapshot()}");
            Step(driver, trace, "TOGGLE");
            Step(driver, trace, "TICK");
            Step(driver, trace, "TICK");
            Step(driver, trace, "TICK");
            Step(driver, trace, "TOGGLE");
        }

        var asClass = Trace("StarterSm", "Machines.StarterSm", asStruct: false, Script);
        var asStruct = Trace("StarterSm", "Machines.StarterSm", asStruct: true, Script);

        Assert.Equal(asClass, asStruct);
    }

    [Fact]
    public void ADefaultStructStartsInTheRootStateSoWeCanTellItIsNotStartedYet()
    {
        var trace = Trace("StarterSm", "Machines.StarterSm", asStruct: true, (driver, lines) => lines.Add(driver.Snapshot()));

        Assert.Equal("ROOT|count=0", trace[0]);
    }
}
