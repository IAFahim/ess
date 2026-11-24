using Xunit;

namespace Ess.Tests;

public sealed class SvRepoTests : IDisposable
{
    private readonly string workFolder = Path.Combine(Path.GetTempPath(), "ess-sv-test-" + Guid.NewGuid().ToString("N"));
    private readonly string originPath;
    private readonly string seedFolder;
    private readonly Subprocess subprocess = new();
    private readonly StringWriter consoleOutput = new();
    private readonly SvRepo repo;

    public SvRepoTests()
    {
        Directory.CreateDirectory(workFolder);
        originPath = Path.Combine(workFolder, "origin.git");
        seedFolder = Path.Combine(workFolder, "seed");

        repo = new SvRepo(subprocess, new EssConsole(consoleOutput, useColor: false), originPath);

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

    [Fact]
    public async Task ClonesThenKnowsItIsAnSvClone()
    {
        var cloned = await repo.Clone(workFolder, "sv", CancellationToken.None);

        Assert.True(cloned);
        Assert.Equal("one", File.ReadAllText(Path.Combine(workFolder, "sv", "README.md")));
        Assert.True(await repo.IsSvClone(Path.Combine(workFolder, "sv"), CancellationToken.None));
        var otherFolder = Path.Combine(workFolder, "other");
        Directory.CreateDirectory(otherFolder);
        Git(otherFolder, "init", "-b", "main");
        Git(otherFolder, "remote", "add", "origin", Path.Combine(workFolder, "somewhere-else.git"));
        Assert.False(await repo.IsSvClone(otherFolder, CancellationToken.None));
        Assert.False(await repo.IsSvClone(Path.Combine(workFolder, "nowhere"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateSaysAlreadyLatestThenPullsNewCommits()
    {
        await repo.Clone(workFolder, "sv", CancellationToken.None);
        var clone = Path.Combine(workFolder, "sv");

        Assert.True(await repo.Update(clone, throwAwayLocalChanges: false, CancellationToken.None));
        Assert.Contains("already the latest", consoleOutput.ToString());

        CommitInSeed("README.md", "two");
        CommitInSeed("NEW.md", "three");

        Assert.True(await repo.Update(clone, throwAwayLocalChanges: false, CancellationToken.None));
        Assert.Contains("2 new commits", consoleOutput.ToString());
        Assert.Equal("two", File.ReadAllText(Path.Combine(clone, "README.md")));
        Assert.True(File.Exists(Path.Combine(clone, "NEW.md")));
    }

    [Fact]
    public async Task UpdateRefusesToOverwriteYourChangesButResetThrowsThemAway()
    {
        await repo.Clone(workFolder, "sv", CancellationToken.None);
        var clone = Path.Combine(workFolder, "sv");

        File.WriteAllText(Path.Combine(clone, "README.md"), "mine");
        File.WriteAllText(Path.Combine(clone, "scratch.txt"), "mine too");
        CommitInSeed("README.md", "theirs");

        Assert.False(await repo.Update(clone, throwAwayLocalChanges: false, CancellationToken.None));
        Assert.Equal("mine", File.ReadAllText(Path.Combine(clone, "README.md")));

        Assert.True(await repo.Update(clone, throwAwayLocalChanges: true, CancellationToken.None));
        Assert.Equal("theirs", File.ReadAllText(Path.Combine(clone, "README.md")));
        Assert.False(File.Exists(Path.Combine(clone, "scratch.txt")));
    }

    [Fact]
    public async Task CloneFailsCleanlyWhenTheSourceIsMissing()
    {
        var missing = new SvRepo(subprocess, new EssConsole(consoleOutput, useColor: false), Path.Combine(workFolder, "no-such-repo.git"));

        Assert.False(await missing.Clone(workFolder, "sv2", CancellationToken.None));
        Assert.Contains("Could not get sv", consoleOutput.ToString());
        Assert.False(Directory.Exists(Path.Combine(workFolder, "sv2")));
    }
}
