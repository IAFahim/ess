namespace Ess;

internal sealed class SvRepo(Subprocess subprocess, EssConsole console, string sourceUrl)
{
    public async Task<bool> IsSvClone(string folder, CancellationToken cancel)
    {
        if (!Directory.Exists(Path.Combine(folder, ".git")))
        {
            return false;
        }

        var origin = await Git(folder, cancel, "remote", "get-url", "origin");
        return origin.Succeeded && origin.Output.Trim() == sourceUrl;
    }

    public async Task<bool> Clone(string parentFolder, string cloneFolderName, CancellationToken cancel)
    {
        console.Step("Getting sv from GitHub.");

        var cloned = await Git(parentFolder, cancel, "clone", sourceUrl, cloneFolderName);
        if (!cloned.Succeeded)
        {
            console.Bad("Could not get sv. Are you online?");
            console.Line(cloned.Output.Trim());
            return false;
        }

        console.Good($"sv is ready at {Path.Combine(parentFolder, cloneFolderName)}");
        return true;
    }

    public async Task<bool> Update(string cloneFolder, bool throwAwayLocalChanges, CancellationToken cancel)
    {
        var before = (await Git(cloneFolder, cancel, "rev-parse", "HEAD")).Output.Trim();

        var fetched = await Git(cloneFolder, cancel, "fetch", "origin");
        if (!fetched.Succeeded)
        {
            console.Bad("Could not reach GitHub. Are you online and logged in?");
            console.Line(fetched.Output.Trim());
            return false;
        }

        var moved = throwAwayLocalChanges
            ? await ResetToLatest(cloneFolder, cancel)
            : await Git(cloneFolder, cancel, "pull", "--ff-only");

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

    private async Task<ProcessResult> ResetToLatest(string cloneFolder, CancellationToken cancel)
    {
        var reset = await Git(cloneFolder, cancel, "reset", "--hard", "@{upstream}");
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
