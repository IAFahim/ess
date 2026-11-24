namespace Ess;

internal sealed class SvRepo(Subprocess subprocess, EssConsole console, string sourceUrl)
{
    public async Task<bool> IsSvClone(string folder, CancellationToken cancel)
    {
        var gitPath = Path.Combine(folder, ".git");
        if (!Directory.Exists(gitPath) && !File.Exists(gitPath))
        {
            return false;
        }

        var origin = await Git(folder, cancel, "remote", "get-url", "origin");
        return origin.Succeeded && origin.Output.Trim() == sourceUrl;
    }

    public async Task<bool> AddAsSubmodule(string projectFolder, string relativePath, bool allowInsideAnotherRepo, CancellationToken cancel)
    {
        if (!await EnsureOwnRepository(projectFolder, allowInsideAnotherRepo, cancel))
        {
            return false;
        }

        console.Step("Getting sv from GitHub, as a git submodule in " + relativePath);

        var added = await Git(projectFolder, cancel, "submodule", "add", sourceUrl, relativePath);
        if (!added.Succeeded)
        {
            console.Bad("Could not get sv. Are you online?");
            console.Line(added.Output.Trim());
            return false;
        }

        console.Good($"sv is ready at {Path.Combine(projectFolder, relativePath)}");
        return true;
    }

    public async Task<bool> Update(string cloneFolder, bool throwAwayLocalChanges, CancellationToken cancel)
    {
        var before = (await Git(cloneFolder, cancel, "rev-parse", "HEAD")).Output.Trim();

        var fetched = await Git(cloneFolder, cancel, "fetch", "origin");
        if (!fetched.Succeeded)
        {
            console.Bad("Could not reach GitHub. Are you online?");
            console.Line(fetched.Output.Trim());
            return false;
        }

        var moved = throwAwayLocalChanges
            ? await ResetToLatest(cloneFolder, cancel)
            : await Git(cloneFolder, cancel, "merge", "--ff-only", "origin/HEAD");

        if (!moved.Succeeded)
        {
            console.Bad("sv has changes of your own, or it moved in a way I will not guess. I did not touch it:");
            console.Line(moved.Output.Trim());
            return false;
        }

        var after = (await Git(cloneFolder, cancel, "rev-parse", "HEAD")).Output.Trim();
        var latest = (await Git(cloneFolder, cancel, "log", "-1", "--format=%h %s")).Output.Trim();

        if (before == after)
        {
            console.Good($"sv is already the latest: {latest}");
            return true;
        }

        var newCommits = (await Git(cloneFolder, cancel, "rev-list", "--count", $"{before}..{after}")).Output.Trim();
        console.Good($"sv updated, {newCommits} new commits. Now at {latest}");
        return true;
    }

    private async Task<bool> EnsureOwnRepository(string folder, bool allowInsideAnotherRepo, CancellationToken cancel)
    {
        if (Directory.Exists(Path.Combine(folder, ".git")))
        {
            return true;
        }

        var prefix = await Git(folder, cancel, "rev-parse", "--show-prefix");
        var insideAnotherRepo = prefix.Succeeded;

        if (insideAnotherRepo && !allowInsideAnotherRepo)
        {
            console.Bad("This folder is inside a bigger git project. Make a folder just for this and run ess there.");
            return false;
        }

        var started = await Git(folder, cancel, "init", "-b", "main");
        if (!started.Succeeded)
        {
            console.Bad(started.Output.Trim());
            return false;
        }

        console.Step("Started a git project in this folder.");
        return true;
    }

    private async Task<ProcessResult> ResetToLatest(string cloneFolder, CancellationToken cancel)
    {
        var reset = await Git(cloneFolder, cancel, "reset", "--hard", "origin/HEAD");
        if (!reset.Succeeded)
        {
            return reset;
        }

        return await Git(cloneFolder, cancel, "clean", "-fd");
    }

    private Task<ProcessResult> Git(string folder, CancellationToken cancel, params string[] arguments)
    {
        return subprocess.Run("git", arguments, folder, cancel);
    }
}
