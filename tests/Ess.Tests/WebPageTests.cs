using Xunit;

namespace Ess.Tests;

public sealed class WebPageTests
{
    private readonly WebPage webPage = new();

    [Theory]
    [InlineData("DoorSm.sim.html", true)]
    [InlineData("rooms/DoorSm.SIM.HTML", true)]
    [InlineData("DoorSm.html", false)]
    [InlineData("DoorSm.cs", false)]
    public void KnowsWhichFilesAreSimulators(string path, bool expected) => Assert.Equal(expected, webPage.IsSimulator(path));

    [Fact]
    public void TheIndexLinksEverySimulatorInOrder()
    {
        var html = webPage.Index("door-game", ["LampSm.sim.html", "DoorSm.sim.html"]);

        Assert.Contains("<title>door-game</title>", html);
        Assert.Contains("<a href=\"DoorSm.sim.html\">DoorSm</a>", html);
        Assert.Contains("<a href=\"LampSm.sim.html\">LampSm</a>", html);
        Assert.True(html.IndexOf("DoorSm", StringComparison.Ordinal) < html.IndexOf("LampSm", StringComparison.Ordinal));
        Assert.DoesNotContain("{links}", html);
        Assert.DoesNotContain("{title}", html);
    }

    [Fact]
    public void TheIndexEscapesOddNames()
    {
        var html = webPage.Index("<me>", ["my rooms/Door&Sm.sim.html"]);

        Assert.Contains("<title>&lt;me&gt;</title>", html);
        Assert.Contains("href=\"my%20rooms/Door%26Sm.sim.html\"", html);
        Assert.Contains(">my rooms/Door&amp;Sm</a>", html);
    }
}
