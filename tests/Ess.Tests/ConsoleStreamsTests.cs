using Xunit;

namespace Ess.Tests;

public class ConsoleStreamsTests
{
    private readonly StringWriter output = new();
    private readonly StringWriter error = new();

    [Fact]
    public void UsageErrorsGoToTheErrorStreamOnly()
    {
        var console = new EssConsole(output, error, useColor: false);

        console.UsageError("Unknown command \"x\".");
        console.UsageHint("See:  ess --help");

        Assert.Equal("", output.ToString());
        Assert.Equal($"error: Unknown command \"x\".{Environment.NewLine}See:  ess --help{Environment.NewLine}", error.ToString());
    }

    [Fact]
    public void NormalMessagesGoToTheOutputStreamOnly()
    {
        var console = new EssConsole(output, error, useColor: false);

        console.Good("done");
        console.Line("plain");

        Assert.Equal("", error.ToString());
        Assert.Contains("ok  done", output.ToString());
    }
}
