using Xunit;

namespace Ess.Tests;

public class DemoTests
{
    private readonly DemoCatalog catalog = new();
    private readonly DemoSelector selector = new(new DemoCatalog());

    [Theory]
    [InlineData("1", "light")]
    [InlineData("2", "player")]
    [InlineData("3", "ecs")]
    [InlineData("4", "sv")]
    [InlineData("light", "light")]
    [InlineData("LIGHT", "light")]
    [InlineData("sv", "sv")]
    [InlineData("-1", "light")]
    [InlineData("--sv", "sv")]
    public void ANumberOrANameFindsTheDemo(string which, string expectedName)
    {
        Assert.Equal(expectedName, selector.Find(which)!.Name);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("5")]
    [InlineData("fast")]
    [InlineData("")]
    public void AnythingElseFindsNothing(string which)
    {
        Assert.Null(selector.Find(which));
    }

    [Fact]
    public void DemosAreNumberedFromOneWithUniqueNamesAndFolders()
    {
        Assert.Equal([1, 2, 3, 4], catalog.All.Select(demo => demo.Number));
        Assert.Equal(catalog.All.Count, catalog.All.Select(demo => demo.Name).Distinct().Count());
        Assert.Equal(catalog.All.Count, catalog.All.Select(demo => demo.FolderName).Distinct().Count());
        Assert.Single(catalog.All, demo => demo.IsSvEngine);
    }

    [Fact]
    public void TheListShowsEveryDemoWithItsNumberAndName()
    {
        var text = string.Join("\n", catalog.ListLines());

        Assert.Contains("ess d <number or name>", text);

        foreach (var demo in catalog.All)
        {
            Assert.Contains($"{demo.Number}  {demo.Name}", text);
            Assert.Contains(demo.Title, text);
        }
    }

    [Theory]
    [InlineData("light", "LightSm.plantuml", "Program.cs", "Light.csproj", "README.txt")]
    [InlineData("player", "PlayerSm.plantuml", "PlayerSm.Actions.cs", "Program.cs", "Player.csproj", "README.txt")]
    [InlineData("ecs", "PlayerSm.plantuml", "PlayerSmEcsTests.cs", "EssStructProbe.Tests.asmdef", "README.txt")]
    public async Task EveryFileDemoWritesItsFiles(string resourceFolder, params string[] expectedFiles)
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
