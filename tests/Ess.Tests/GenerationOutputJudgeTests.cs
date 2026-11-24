using Xunit;

namespace Ess.Tests;

public class GenerationOutputJudgeTests
{
    private readonly GenerationOutputJudge judge = new();

    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void RealGoodRunSucceedsAndListsEveryWrittenFile()
    {
        var result = new ProcessResult(0, Fixture("stateSmith-good.txt"));

        Assert.True(judge.Succeeded(result));
        Assert.Equal(["LightSm.md", "LightSm.cs", "LightSm.sim.html"], judge.FilesWritten(result.Output));
    }

    [Fact]
    public void RealBrokenDiagramFailsEvenThoughStateSmithExitsWithZero()
    {
        var result = new ProcessResult(0, Fixture("stateSmith-bad.txt"));

        Assert.False(judge.Succeeded(result));
        Assert.Empty(judge.FilesWritten(result.Output));
    }

    [Fact]
    public void ProblemShowsOnlyTheReasonNotTheChatter()
    {
        var problem = judge.Problem(Fixture("stateSmith-bad.txt"));

        Assert.Contains("no viable alternative", problem);
        Assert.DoesNotContain("settings directory", problem);
        Assert.DoesNotContain("Related error info", problem);
        Assert.DoesNotContain("Finished with failure", problem);
    }

    [Fact]
    public void ProblemFallsBackToEverythingWhenTheOutputIsNotRecognised()
    {
        Assert.Equal("something odd", judge.Problem("  something odd\n"));
    }

    [Fact]
    public void ANonZeroExitCodeAlwaysFails()
    {
        Assert.False(judge.Succeeded(new ProcessResult(1, "StateSmith Runner - Finished normally.")));
    }
}
