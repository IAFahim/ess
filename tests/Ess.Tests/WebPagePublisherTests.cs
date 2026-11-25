using Xunit;

namespace Ess.Tests;

public sealed class WebPagePublisherTests : IDisposable
{
    private readonly string workFolder = Path.Combine(Path.GetTempPath(), "ess-pages-test-" + Guid.NewGuid().ToString("N"));
    private readonly string originPath;
    private readonly string projectFolder;
    private readonly Subprocess subprocess = new();
    private readonly StringWriter consoleOutput = new();
    private readonly WebPagePublisher publisher;

    public WebPagePublisherTests()
    {
        Directory.CreateDirectory(workFolder);
        originPath = Path.Combine(workFolder, "origin.git");
        projectFolder = Path.Combine(workFolder, "door-game");
        Directory.CreateDirectory(projectFolder);

        publisher = new WebPagePublisher(subprocess, new EssConsole(consoleOutput, consoleOutput, useColor: false), new WebPage());

        Git(workFolder, "init", "-q", "--bare", "-b", "main", originPath);
        Git(projectFolder, "init", "-q", "-b", "main");
        Git(projectFolder, "remote", "add", "origin", originPath);
        Git(projectFolder, "config", "user.name", "test");
        Git(projectFolder, "config", "user.email", "test@example.invalid");
    }

    public void Dispose() => Directory.Delete(workFolder, recursive: true);

    private string Git(string folder, params string[] arguments)
    {
        var result = subprocess.Run("git", arguments, folder, CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(result.Succeeded, $"git {string.Join(' ', arguments)}: {result.Output}");
        return result.Output;
    }

    private void CommitAndPush(params (string Path, string Content)[] files)
    {
        foreach (var (path, content) in files)
        {
            var fullPath = Path.Combine(projectFolder, path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }

        Git(projectFolder, "add", "-A");
        Git(projectFolder, "commit", "-q", "-m", "work");
        Git(projectFolder, "push", "-q", "origin", "HEAD:main");
    }

    private Task<PublishOutcome> Publish() => publisher.Publish(projectFolder, CancellationToken.None);

    private string[] PublishedFiles() =>
        Git(originPath, "ls-tree", "-r", "--name-only", WebPagePublisher.BranchName)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private string PublishedText(string path) => Git(originPath, "show", $"{WebPagePublisher.BranchName}:{path}").TrimEnd('\n');

    private string CommitCount() => Git(originPath, "rev-list", "--count", WebPagePublisher.BranchName).Trim();

    [Fact]
    public async Task OnlyTheSimulatorsAndAnIndexGoOnTheWebPage()
    {
        CommitAndPush(("DoorSm.plantuml", "@startuml DoorSm\n@enduml\n"), ("DoorSm.cs", "code"), ("DoorSm.sim.html", "door"), ("rooms/LampSm.sim.html", "lamp"));

        Assert.Equal(PublishOutcome.Published, await Publish());

        Assert.Equal([".nojekyll", "DoorSm.sim.html", "index.html", "rooms/LampSm.sim.html"], PublishedFiles());
        Assert.Equal("door", PublishedText("DoorSm.sim.html"));
        Assert.Contains("href=\"rooms/LampSm.sim.html\"", PublishedText("index.html"));
    }

    [Fact]
    public async Task PublishingAgainWithoutChangesMakesNoNewCommit()
    {
        CommitAndPush(("DoorSm.sim.html", "door"));
        Assert.Equal(PublishOutcome.Published, await Publish());

        Assert.Equal(PublishOutcome.UpToDate, await Publish());

        Assert.Equal("1", CommitCount());
    }

    [Fact]
    public async Task AChangedSimulatorAddsOneCommitOnTop()
    {
        CommitAndPush(("DoorSm.sim.html", "door"));
        await Publish();
        CommitAndPush(("DoorSm.sim.html", "door with a lock"));

        Assert.Equal(PublishOutcome.Published, await Publish());

        Assert.Equal("2", CommitCount());
        Assert.Equal("door with a lock", PublishedText("DoorSm.sim.html"));
    }

    [Fact]
    public async Task ItDoesNotTouchYourFilesOrYourIndex()
    {
        CommitAndPush(("DoorSm.sim.html", "door"));
        File.WriteAllText(Path.Combine(projectFolder, "notes.txt"), "not saved yet");

        await Publish();

        Assert.Equal("?? notes.txt", Git(projectFolder, "status", "--porcelain").Trim());
        Assert.Equal("main", Git(projectFolder, "branch", "--show-current").Trim());
    }

    [Fact]
    public async Task NoSimulatorsMeansNoWebPage()
    {
        CommitAndPush(("DoorSm.plantuml", "@startuml DoorSm\n@enduml\n"));

        Assert.Equal(PublishOutcome.NoSimulators, await Publish());

        Assert.DoesNotContain(WebPagePublisher.BranchName, Git(originPath, "branch", "--list"));
    }
}
