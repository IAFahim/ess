using Xunit;

namespace Ess.Tests;

public sealed class DiagramLookupTests : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("ess-lookup-").FullName;
    private readonly DiagramLookup lookup = new(new DiagramFiles(), new EditDistance());

    public DiagramLookupTests()
    {
        File.WriteAllText(Path.Combine(folder, "Hero.plantuml"), "@startuml\n@enduml\n");
        File.WriteAllText(Path.Combine(folder, "Door.puml"), "@startuml\n@enduml\n");
        File.WriteAllText(Path.Combine(folder, "CounterSm.plantuml"), "@startuml\n@enduml\n");
        File.WriteAllText(Path.Combine(folder, "LampSm.plantuml"), "@startuml\n@enduml\n");
        File.WriteAllText(Path.Combine(folder, "LadderSm.plantuml"), "@startuml\n@enduml\n");
        File.WriteAllText(Path.Combine(folder, "notes.txt"), "");
    }

    public void Dispose() => Directory.Delete(folder, recursive: true);

    [Theory]
    [InlineData("Hero.plantuml", "Hero.plantuml")]
    [InlineData("hero.plantuml", "Hero.plantuml")]
    [InlineData("hero", "Hero.plantuml")]
    [InlineData("Door", "Door.puml")]
    [InlineData("counter", "CounterSm.plantuml")]
    [InlineData("Lamp", "LampSm.plantuml")]
    public void ANameThatClearlyMeansOneDiagramOpensIt(string wanted, string expected)
    {
        var match = lookup.Find(wanted, folder);

        Assert.Equal(Path.Combine(folder, expected), match.Path);
    }

    [Fact]
    public void AFolderOpensAsItIs()
    {
        Assert.Equal(folder, lookup.Find(".", folder).Path);
    }

    [Theory]
    [InlineData("Hreo.plantuml", "Hero.plantuml")]
    [InlineData("hro", "Hero.plantuml")]
    [InlineData("dor", "Door.puml")]
    [InlineData("cuonter", "CounterSm.plantuml")]
    public void ATypoIsNotOpenedButSuggested(string wanted, string expected)
    {
        var match = lookup.Find(wanted, folder);

        Assert.Null(match.Path);
        Assert.Equal(expected, match.Suggestion);
    }

    [Theory]
    [InlineData("l")]
    [InlineData("la")]
    public void AStartTooShortIsNeverOpened(string wanted)
    {
        Assert.Null(lookup.Find(wanted, folder).Path);
    }

    [Fact]
    public void AStartThatFitsTwoDiagramsOpensNeither()
    {
        File.WriteAllText(Path.Combine(folder, "LampPost.plantuml"), "@startuml\n@enduml\n");

        Assert.Null(lookup.Find("lamp", folder).Path);
    }

    [Theory]
    [InlineData("Spaceship")]
    [InlineData("missing/Hero.plantuml")]
    public void SomethingFarAwayGivesNoGuess(string wanted)
    {
        var match = lookup.Find(wanted, folder);

        Assert.Null(match.Path);
        Assert.Null(match.Suggestion);
    }
}
