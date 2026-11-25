using Xunit;

namespace Ess.Tests;

public class ArgumentParserTests
{
    private readonly CommandTable table = new();
    private readonly ArgumentParser parser = new(new CommandTable(), new EditDistance());

    [Theory]
    [InlineData("new", EssCommand.New)]
    [InlineData("n", EssCommand.New)]
    [InlineData("in", EssCommand.New)]
    [InlineData("run", EssCommand.Run)]
    [InlineData("r", EssCommand.Run)]
    [InlineData("watch", EssCommand.Watch)]
    [InlineData("w", EssCommand.Watch)]
    [InlineData("git", EssCommand.Git)]
    [InlineData("g", EssCommand.Git)]
    [InlineData("save", EssCommand.Git)]
    [InlineData("s", EssCommand.Git)]
    [InlineData("demo", EssCommand.Demo)]
    [InlineData("d", EssCommand.Demo)]
    [InlineData("update", EssCommand.Update)]
    [InlineData("open", EssCommand.Open)]
    [InlineData("o", EssCommand.Open)]
    [InlineData("omaspec", EssCommand.Open)]
    public void CommandsAndTheirShortFormsRun(string word, EssCommand expected)
    {
        var args = expected == EssCommand.Update ? new[] { word, "sv" } : new[] { word };

        var invocation = parser.Parse(args);

        Assert.Equal(InvocationKind.Command, invocation.Kind);
        Assert.Equal(expected, invocation.Command!.Command);
    }

    [Theory]
    [InlineData("W")]
    [InlineData("Watch")]
    public void CommandWordsIgnoreCase(string word)
    {
        Assert.Equal(EssCommand.Watch, parser.Parse([word]).Command!.Command);
    }

    [Fact]
    public void NoArgumentsIsItsOwnKind()
    {
        Assert.Equal(InvocationKind.NoArguments, parser.Parse([]).Kind);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    [InlineData("--HELP")]
    public void HelpAloneIsGlobalHelp(string flag)
    {
        Assert.Equal(InvocationKind.GlobalHelp, parser.Parse([flag]).Kind);
    }

    [Theory]
    [InlineData("d", "--help")]
    [InlineData("d", "-h")]
    [InlineData("--help", "d")]
    [InlineData("d", "1", "--help")]
    public void HelpWorksBeforeOrAfterACommand(params string[] args)
    {
        var invocation = parser.Parse(args);

        Assert.Equal(InvocationKind.CommandHelp, invocation.Kind);
        Assert.Equal(EssCommand.Demo, invocation.Command!.Command);
    }

    [Fact]
    public void VersionIsOnlyDashDashVersion()
    {
        Assert.Equal(InvocationKind.Version, parser.Parse(["--version"]).Kind);
        Assert.Equal(InvocationKind.Error, parser.Parse(["-v"]).Kind);
        Assert.Equal(InvocationKind.Error, parser.Parse(["version"]).Kind);
    }

    [Theory]
    [InlineData("wach", EssCommand.Watch)]
    [InlineData("wathc", EssCommand.Watch)]
    [InlineData("gti", EssCommand.Git)]
    [InlineData("rnu", EssCommand.Run)]
    [InlineData("neww", EssCommand.New)]
    [InlineData("dmeo", EssCommand.Demo)]
    [InlineData("updte", EssCommand.Update)]
    [InlineData("omaspce", EssCommand.Open)]
    [InlineData("opne", EssCommand.Open)]
    public void SmallTyposAreErrorsWithASuggestion(string word, EssCommand expected)
    {
        var invocation = parser.Parse([word]);

        Assert.Equal(InvocationKind.Error, invocation.Kind);
        Assert.Contains("Unknown command", invocation.Error);
        Assert.Equal(expected, invocation.SuggestedCommand!.Command);
    }

    [Theory]
    [InlineData("dsv", "ess d sv")]
    [InlineData("d1", "ess d")]
    [InlineData("d12", "ess d")]
    public void OldFormsPointAtTheNewOne(string word, string expectedLine)
    {
        var invocation = parser.Parse([word]);

        Assert.Equal(InvocationKind.Error, invocation.Kind);
        Assert.Equal(expectedLine, invocation.SuggestedLine);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("zz")]
    [InlineData("banana")]
    [InlineData("help")]
    [InlineData("menu")]
    [InlineData("wa")]
    public void NonsenseAndGuessesAreErrorsWithNoSuggestion(string word)
    {
        var invocation = parser.Parse([word]);

        Assert.Equal(InvocationKind.Error, invocation.Kind);
        Assert.Null(invocation.SuggestedCommand);
        Assert.Equal("", invocation.SuggestedLine);
    }

    [Fact]
    public void AnUnknownOptionIsAnError()
    {
        var invocation = parser.Parse(["r", "--fast"]);

        Assert.Equal(InvocationKind.Error, invocation.Kind);
        Assert.Contains("Unknown option \"--fast\"", invocation.Error);
        Assert.Contains("ess run --help", invocation.Error);
    }

    [Fact]
    public void AnUnknownOptionAloneIsAnError()
    {
        Assert.Equal(InvocationKind.Error, parser.Parse(["--bogus"]).Kind);
    }

    [Theory]
    [InlineData("-1", "-1")]
    [InlineData("1", "1")]
    [InlineData("light", "light")]
    [InlineData("sv", "sv")]
    public void ADemoTakesOneArgumentAndANegativeNumberIsNotAnOption(string typed, string expected)
    {
        var invocation = parser.Parse(["d", typed]);

        Assert.Equal(InvocationKind.Command, invocation.Kind);
        Assert.Equal([expected], invocation.Arguments);
    }

    [Fact]
    public void ArgumentsAfterDashDashAreNeverOptions()
    {
        var invocation = parser.Parse(["d", "--", "--odd"]);

        Assert.Equal(InvocationKind.Command, invocation.Kind);
        Assert.Equal(["--odd"], invocation.Arguments);
    }

    [Theory]
    [InlineData("r", "extra")]
    [InlineData("w", "extra")]
    [InlineData("g", "extra")]
    [InlineData("n", "a", "b")]
    [InlineData("d", "1", "2")]
    [InlineData("u", "sv", "x")]
    public void TooManyArgumentsIsAnError(params string[] args)
    {
        var invocation = parser.Parse(args);

        Assert.Equal(InvocationKind.Error, invocation.Kind);
        Assert.Contains("ess", invocation.Error);
    }

    [Fact]
    public void UpdateNeedsAThingToUpdate()
    {
        var invocation = parser.Parse(["u"]);

        Assert.Equal(InvocationKind.Error, invocation.Kind);
        Assert.Contains("needs <what>", invocation.Error);
        Assert.Contains("ess u sv", invocation.Error);
    }

    [Fact]
    public void EveryExampleInTheHelpParsesToItsOwnCommand()
    {
        foreach (var spec in table.All)
        {
            foreach (var example in spec.Examples)
            {
                var invocation = parser.Parse(example.Split(' ').Skip(1).ToArray());

                Assert.True(invocation.Kind == InvocationKind.Command, $"{example}: {invocation.Error}");
                Assert.Equal(spec.Command, invocation.Command!.Command);
            }
        }
    }

    [Fact]
    public void EveryGlobalExampleParses()
    {
        foreach (var example in table.GlobalExamples)
        {
            var invocation = parser.Parse(example.Split(' ').Skip(1).ToArray());

            Assert.True(invocation.Kind == InvocationKind.Command, $"{example}: {invocation.Error}");
        }
    }

    [Fact]
    public void EveryCommandHasExactlyOneSingleLetterKeyAndNoWordIsUsedTwice()
    {
        var words = table.All.SelectMany(spec => spec.Words).ToList();

        Assert.Equal(words.Count, words.Distinct().Count());

        foreach (var spec in table.All)
        {
            Assert.Single(spec.Aliases.Where(alias => alias.Length == 1).Take(1));
            Assert.Equal(1, spec.Key.Length);
        }
    }
}
