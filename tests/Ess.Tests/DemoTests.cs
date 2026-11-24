using Xunit;

namespace Ess.Tests;

public class DemoTests
{
    private readonly DemoRequest request = new();
    private readonly DemoCatalog catalog = new();

    [Theory]
    [InlineData("d", 0)]
    [InlineData("D", 0)]
    [InlineData("d1", 1)]
    [InlineData("d2", 2)]
    [InlineData("--d2", 2)]
    [InlineData("d12", 12)]
    public void TheLevelComesFromTheTypedWord(string typed, int expected)
    {
        Assert.Equal(expected, request.LevelFrom(typed, []));
    }

    [Theory]
    [InlineData("demo", "2", 2)]
    [InlineData("demo", "0", 0)]
    [InlineData("d", "1", 1)]
    public void ALevelCanFollowTheWord(string typed, string extra, int expected)
    {
        Assert.Equal(expected, request.LevelFrom(typed, [extra]));
    }

    [Theory]
    [InlineData("dsv", null, true)]
    [InlineData("DSV", null, true)]
    [InlineData("demo", "sv", true)]
    [InlineData("demo", "1", false)]
    [InlineData("d1", null, false)]
    [InlineData("d", null, false)]
    public void TheSvDemoIsDsvOrDemoSv(string typed, string? extra, bool expected)
    {
        Assert.Equal(expected, request.IsSvDemo(typed, extra is null ? [] : [extra]));
    }

    [Fact]
    public void DsvIsADemoWordButHasNoLevel()
    {
        Assert.True(request.IsDemoWord("dsv"));
        Assert.Null(request.LevelFrom("dsv", []));
    }

    [Fact]
    public void WithNoLevelTheBasicDemoIsMeant()
    {
        Assert.Equal(0, request.LevelFrom("demo", []));
    }

    [Fact]
    public void ANonNumberAfterTheWordIsNotADemo()
    {
        Assert.Null(request.LevelFrom("demo", ["fast"]));
    }

    [Theory]
    [InlineData("d", true)]
    [InlineData("d7", true)]
    [InlineData("demo", false)]
    [InlineData("dx", false)]
    [InlineData("", false)]
    [InlineData("d12345", false)]
    public void OnlyDThenDigitsIsADemoWord(string word, bool expected)
    {
        Assert.Equal(expected, request.IsDemoWord(word));
    }

    [Fact]
    public void EveryDemoHasAFolderAndASequentialLevel()
    {
        Assert.Equal([0, 1, 2], catalog.All.Select(demo => demo.Level));
        Assert.Equal(catalog.All.Count, catalog.All.Select(demo => demo.FolderName).Distinct().Count());
        Assert.Null(catalog.ForLevel(3));
    }

    [Theory]
    [InlineData("light", "LightSm.plantuml", "Program.cs", "Light.csproj", "README.txt")]
    [InlineData("player", "PlayerSm.plantuml", "PlayerSm.Actions.cs", "Program.cs", "Player.csproj", "README.txt")]
    [InlineData("ecs", "PlayerSm.plantuml", "PlayerSmEcsTests.cs", "EssStructProbe.Tests.asmdef", "README.txt")]
    public async Task EveryDemoWritesItsFiles(string resourceFolder, params string[] expectedFiles)
    {
        var target = Path.Combine(Path.GetTempPath(), "ess-demo-test-" + Guid.NewGuid().ToString("N"));

        try
        {
            var written = await new DemoFiles().WriteTo(target, resourceFolder, CancellationToken.None);

            foreach (var expected in expectedFiles)
            {
                Assert.Contains(expected, written);
                Assert.True(File.Exists(Path.Combine(target, expected)));
            }
        }
        finally
        {
            Directory.Delete(target, recursive: true);
        }
    }
}
