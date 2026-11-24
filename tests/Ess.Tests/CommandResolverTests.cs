using Xunit;

namespace Ess.Tests;

public class CommandResolverTests
{
    private readonly CommandResolver resolver = new(new CommandCatalog(), new EditDistance());

    [Theory]
    [InlineData("n", EssCommand.New)]
    [InlineData("new", EssCommand.New)]
    [InlineData("init", EssCommand.New)]
    [InlineData("i", EssCommand.New)]
    [InlineData("r", EssCommand.Run)]
    [InlineData("run", EssCommand.Run)]
    [InlineData("w", EssCommand.Watch)]
    [InlineData("watch", EssCommand.Watch)]
    [InlineData("g", EssCommand.Git)]
    [InlineData("git", EssCommand.Git)]
    [InlineData("save", EssCommand.Git)]
    [InlineData("s", EssCommand.Git)]
    [InlineData("in", EssCommand.New)]
    public void ExactWordsRunTheCommand(string typed, EssCommand expected)
    {
        var resolution = resolver.Resolve(typed);

        Assert.Equal(ResolutionKind.RunCommand, resolution.Kind);
        Assert.Equal(expected, resolution.Command!.Command);
    }

    [Theory]
    [InlineData("W", EssCommand.Watch)]
    [InlineData("  Watch ", EssCommand.Watch)]
    [InlineData("--watch", EssCommand.Watch)]
    [InlineData("wa", EssCommand.Watch)]
    [InlineData("ru", EssCommand.Run)]
    public void CapitalsSpacesDashesAndPrefixesAreForgiven(string typed, EssCommand expected)
    {
        var resolution = resolver.Resolve(typed);

        Assert.Equal(ResolutionKind.RunCommand, resolution.Kind);
        Assert.Equal(expected, resolution.Command!.Command);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?")]
    [InlineData("help")]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("menu")]
    public void HelpAndNothingShowTheMenu(string typed)
    {
        Assert.Equal(ResolutionKind.ShowMenu, resolver.Resolve(typed).Kind);
    }

    [Theory]
    [InlineData("v")]
    [InlineData("-v")]
    [InlineData("--version")]
    [InlineData("version")]
    public void VersionWordsShowTheVersion(string typed)
    {
        Assert.Equal(ResolutionKind.ShowVersion, resolver.Resolve(typed).Kind);
    }

    [Theory]
    [InlineData("wach", EssCommand.Watch)]
    [InlineData("wathc", EssCommand.Watch)]
    [InlineData("gti", EssCommand.Git)]
    [InlineData("rnu", EssCommand.Run)]
    [InlineData("neww", EssCommand.New)]
    public void SmallTyposAskDidYouMean(string typed, EssCommand expected)
    {
        var resolution = resolver.Resolve(typed);

        Assert.Equal(ResolutionKind.DidYouMean, resolution.Kind);
        Assert.Equal(expected, resolution.Command!.Command);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("zz")]
    [InlineData("banana")]
    public void NonsenseIsUnknown(string typed)
    {
        Assert.Equal(ResolutionKind.Unknown, resolver.Resolve(typed).Kind);
    }
}
