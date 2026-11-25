using Xunit;

namespace Ess.Tests;

public class HelpWriterTests
{
    private readonly CommandTable table = new();
    private readonly HelpWriter help = new(new CommandTable());

    [Fact]
    public void GlobalHelpListsEveryCommandWithItsShortFormsAndSummary()
    {
        var text = string.Join("\n", help.Global());

        Assert.Contains("Usage: ess <command> [arguments] [options]", text);
        Assert.Contains("--version", text);

        foreach (var spec in table.All)
        {
            Assert.Contains(string.Join(", ", spec.Words), text);
            Assert.Contains(spec.Brief, text);
        }
    }

    [Fact]
    public void CommandHelpHasUsageExamplesAndTheHelpOption()
    {
        var spec = table.Find("d")!;

        var text = string.Join("\n", help.For(spec));

        Assert.Contains("Usage: ess demo [which] [options]", text);
        Assert.Contains("ess d sv", text);
        Assert.Contains("Short forms: d", text);
        Assert.Contains("-h, --help", text);
    }

    [Fact]
    public void ACommandWithNoArgumentsHasNoDoubleSpaceInItsUsage()
    {
        var text = string.Join("\n", help.For(table.Find("r")!));

        Assert.Contains("Usage: ess run [options]", text);
    }

    [Fact]
    public void TheMenuOffersOnlyCommandsThatNeedNoArgument()
    {
        var menu = string.Join("\n", help.Menu());

        Assert.Contains("n   new", menu);
        Assert.Contains("d   demo", menu);
        Assert.Contains("o   open", menu);
        Assert.DoesNotContain("update", menu);
    }
}
