using Xunit;

namespace Ess.Tests;

public sealed class SvRepoTests : IDisposable
{
    private readonly string workFolder = Path.Combine(Path.GetTempPath(), "ess-sv-test-" + Guid.NewGuid().ToString("N"));
    private readonly string originPath;
    private readonly string seedFolder;
    private readonly string projectFolder;
    private readonly string svFolder;
    private readonly Subprocess subprocess = new();
    private readonly StringWriter consoleOutput = new();
    private readonly SvRepo repo;

    public SvRepoTests()
    {
        Environment.SetEnvironmentVariable("GIT_ALLOW_PROTOCOL", "file:https:ssh:git");

        Directory.CreateDirectory(workFolder);
        originPath = Path.Combine(workFolder, "origin.git");
        seedFolder = Path.Combine(workFolder, "seed");
        projectFolder = Path.Combine(workFolder, "project");
        svFolder = Path.Combine(projectFolder, "lib", "sv");
        Directory.CreateDirectory(projectFolder);

        repo = new SvRepo(subprocess, new EssConsole(consoleOutput, consoleOutput, useColor: false), originPath);

        Git(workFolder, "init", "--bare", "-b", "main", originPath);
        Git(workFolder, "clone", originPath, seedFolder);
        CommitInSeed("README.md", "one");
    }

    public void Dispose() => Directory.Delete(workFolder, recursive: true);

    private void Git(string folder, params string[] arguments)
    {
        var result = subprocess.Run("git", arguments, folder, CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(result.Succeeded, $"git {string.Join(' ', arguments)}: {result.Output}");
    }

    private void CommitInSeed(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(seedFolder, fileName), content);
        Git(seedFolder, "add", "-A");
        Git(seedFolder, "-c", "user.name=test", "-c", "user.email=test@example.invalid", "commit", "-m", $"{fileName} is {content}");
        Git(seedFolder, "push", "origin", "HEAD:main");
    }

    private async Task AddSv() => Assert.True(await repo.AddAsSubmodule(projectFolder, "lib/sv", allowInsideAnotherRepo: false, CancellationToken.None));

    [Fact]
    public async Task AddsSvAsASubmoduleInItsOwnRepositoryAndRecognisesIt()
    {
        await AddSv();

        Assert.True(Directory.Exists(Path.Combine(projectFolder, ".git")));
        Assert.Contains("lib/sv", File.ReadAllText(Path.Combine(projectFolder, ".gitmodules")));
        Assert.Equal("one", File.ReadAllText(Path.Combine(svFolder, "README.md")));
        Assert.True(File.Exists(Path.Combine(svFolder, ".git")));
        Assert.True(await repo.IsSvClone(svFolder, CancellationToken.None));
        Assert.False(await repo.IsSvClone(projectFolder, CancellationToken.None));
        Assert.False(await repo.IsSvClone(Path.Combine(workFolder, "nowhere"), CancellationToken.None));
    }

    [Fact]
    public async Task RefusesToStartInsideAnotherRepositoryUnlessTold()
    {
        var outer = Path.Combine(workFolder, "outer");
        var inner = Path.Combine(outer, "inner");
        Directory.CreateDirectory(inner);
        Git(outer, "init", "-b", "main");

        Assert.False(await repo.AddAsSubmodule(inner, "lib/sv", allowInsideAnotherRepo: false, CancellationToken.None));
        Assert.Contains("inside a bigger git project", consoleOutput.ToString());
        Assert.False(Directory.Exists(Path.Combine(inner, ".git")));

        Assert.True(await repo.AddAsSubmodule(inner, "lib/sv", allowInsideAnotherRepo: true, CancellationToken.None));
        Assert.True(Directory.Exists(Path.Combine(inner, ".git")));
    }

    [Fact]
    public async Task UpdateSaysAlreadyLatestThenFollowsNewCommitsEvenWhenDetached()
    {
        await AddSv();

        Assert.True(await repo.Update(svFolder, throwAwayLocalChanges: false, CancellationToken.None));
        Assert.Contains("already the latest", consoleOutput.ToString());

        CommitInSeed("README.md", "two");
        CommitInSeed("NEW.md", "three");

        Assert.True(await repo.Update(svFolder, throwAwayLocalChanges: false, CancellationToken.None));
        Assert.Contains("2 new commits", consoleOutput.ToString());
        Assert.Equal("two", File.ReadAllText(Path.Combine(svFolder, "README.md")));
        Assert.True(File.Exists(Path.Combine(svFolder, "NEW.md")));
    }

    [Fact]
    public async Task UpdateRefusesToOverwriteYourChangesButResetThrowsThemAway()
    {
        await AddSv();

        File.WriteAllText(Path.Combine(svFolder, "README.md"), "mine");
        File.WriteAllText(Path.Combine(svFolder, "scratch.txt"), "mine too");
        CommitInSeed("README.md", "theirs");

        Assert.False(await repo.Update(svFolder, throwAwayLocalChanges: false, CancellationToken.None));
        Assert.Equal("mine", File.ReadAllText(Path.Combine(svFolder, "README.md")));

        Assert.True(await repo.Update(svFolder, throwAwayLocalChanges: true, CancellationToken.None));
        Assert.Equal("theirs", File.ReadAllText(Path.Combine(svFolder, "README.md")));
        Assert.False(File.Exists(Path.Combine(svFolder, "scratch.txt")));
    }

    [Fact]
    public async Task AddingFailsCleanlyWhenTheSourceIsMissing()
    {
        var missing = new SvRepo(subprocess, new EssConsole(consoleOutput, consoleOutput, useColor: false), Path.Combine(workFolder, "no-such-repo.git"));

        Assert.False(await missing.AddAsSubmodule(projectFolder, "lib/sv", allowInsideAnotherRepo: false, CancellationToken.None));
        Assert.Contains("Could not get sv", consoleOutput.ToString());
        Assert.False(Directory.Exists(svFolder));
    }
}
