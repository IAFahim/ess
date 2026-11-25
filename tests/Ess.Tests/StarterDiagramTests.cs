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

    [Fact]
    public void PlainStarterKeepsTheComponentLineSwitchedOff()
    {
        var text = new StarterDiagram().Render("DoorSm");

        Assert.Contains("# BaseList = \"Unity.Entities.IComponentData\"", text);
        Assert.DoesNotContain("{", text.Split("/'!")[0]);
    }

    [Fact]
    public void ComponentStarterSwitchesTheComponentLineOn()
    {
        var text = new StarterDiagram().Render("DoorSm", asComponent: true);

        Assert.Contains("\nBaseList = \"Unity.Entities.IComponentData\"", text);
        Assert.DoesNotContain("# BaseList", text);
        Assert.DoesNotContain("{baseList}", text);
        Assert.DoesNotContain("{structNote}", text);
    }
}
