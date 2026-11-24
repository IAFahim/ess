using Xunit;

namespace Ess.Tests;

public class StarterDiagramTests
{
    [Fact]
    public void StarterNamesTheMachineAndKeepsItsConfigBlock()
    {
        var text = new StarterDiagram().Render("DoorSm");

        Assert.StartsWith("@startuml DoorSm", text);
        Assert.Contains("transpilerId = \"CSharp\"", text);
        Assert.DoesNotContain("{name}", text);
        Assert.EndsWith("@enduml\n", text);
    }
}
