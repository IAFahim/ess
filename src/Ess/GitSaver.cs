namespace Ess;

internal enum SaveOutcome
{
    Pushed,
    SavedOnThisComputerOnly,
    NothingNew,
    NeedsHelp,
}

internal sealed class GitSaver(Subprocess subprocess, EssConsole console, RepoNames repoNames)
{
    private const string IgnoreFileName = ".gitignore";

    private readonly string[] namesGitShouldIgnore = [".DS_Store", "Thumbs.db", "*.swp", "*~"];

    public bool IsOwnRepository(string folder) => Directory.Exists(Path.Combine(folder, ".git"));

    public async Task<SaveOutcome> Save(string folder, string message, bool createGitHubRepoWhenMissing, CancellationToken cancel)
    {
        var gitVersion = await Git(folder, cancel, "--version");
        if (gitVersion.ProgramMissing)
        {
            console.Bad("git is not installed. Get it from https://git-scm.com/downloads then run this again.");
            return SaveOutcome.NeedsHelp;
        }

        var insideRepository = await Git(folder, cancel, "rev-parse", "--show-prefix");
        var insideAnotherProject = insideRepository.Succeeded && insideRepository.Output.Trim().Length > 0;
        if (insideAnotherProject)
        {
            console.Bad("This folder is inside a bigger git project. Make a folder just for your diagrams and run ess there.");
            return SaveOutcome.NeedsHelp;
        }

        if (!insideRepository.Succeeded)
        {
            var started = await Git(folder, cancel, "init", "-b", "main");
            if (!started.Succeeded)
            {
                console.Bad(started.Output.Trim());
                return SaveOutcome.NeedsHelp;
            }

            console.Step("Started a git project in this folder.");
        }

        if (!await EnsureGitKnowsWhoYouAre(folder, cancel))
        {
            return SaveOutcome.NeedsHelp;
        }

        await WriteIgnoreFileIfMissing(folder, cancel);

        await Git(folder, cancel, "add", "-A");
        var pending = await Git(folder, cancel, "status", "--porcelain");
        var hasNewWork = pending.Output.Trim().Length > 0;

        if (hasNewWork)
        {
            var committed = await Git(folder, cancel, "commit", "-m", message);
            if (!committed.Succeeded)
            {
                console.Bad(committed.Output.Trim());
                return SaveOutcome.NeedsHelp;
            }

            console.Good("Saved on this computer.");
        }

        var hasAnySavedWork = (await Git(folder, cancel, "rev-parse", "--verify", "HEAD")).Succeeded;
        if (!hasAnySavedWork)
        {
            console.Warn("There is nothing to save yet. Run:  ess n");
            return SaveOutcome.NothingNew;
        }

        var remotes = await Git(folder, cancel, "remote");
        var hasRemote = remotes.Output.Trim().Length > 0;

        return hasRemote
            ? await Push(folder, hasNewWork, cancel)
            : await CreateGitHubRepo(folder, hasNewWork, createGitHubRepoWhenMissing, cancel);
    }

    private async Task<SaveOutcome> Push(string folder, bool hasNewWork, CancellationToken cancel)
    {
        var push = await Git(folder, cancel, "push", "-u", "origin", "HEAD");
        if (!push.Succeeded)
        {
            console.Warn("Could not push. Are you online? Your work is saved on this computer. Run ess g again later.");
            return SaveOutcome.SavedOnThisComputerOnly;
        }

        var alreadyUpToDate = push.Output.Contains("Everything up-to-date", StringComparison.Ordinal);
        if (alreadyUpToDate && !hasNewWork)
        {
            console.Good("Nothing new. GitHub is already up to date.");
            return SaveOutcome.NothingNew;
        }

        console.Good("Pushed to GitHub.");
        return SaveOutcome.Pushed;
    }

    private async Task<SaveOutcome> CreateGitHubRepo(string folder, bool hasNewWork, bool createWhenMissing, CancellationToken cancel)
    {
        if (!createWhenMissing)
        {
            return hasNewWork ? SaveOutcome.SavedOnThisComputerOnly : SaveOutcome.NothingNew;
        }

        var ghVersion = await GitHub(folder, cancel, "--version");
        if (ghVersion.ProgramMissing)
        {
            console.Warn("To put this on GitHub, install gh from https://cli.github.com then run ess g again.");
            console.Line("    Your work is saved on this computer.");
            return SaveOutcome.SavedOnThisComputerOnly;
        }

        var ghLogin = await GitHub(folder, cancel, "auth", "status");
        if (!ghLogin.Succeeded)
        {
            console.Warn("gh is not logged in. Run:  gh auth login   then run ess g again.");
            console.Line("    Your work is saved on this computer.");
            return SaveOutcome.SavedOnThisComputerOnly;
        }

        var repoName = repoNames.FromFolder(folder);
        var created = await GitHub(folder, cancel, "repo", "create", repoName, "--private", "--source", ".", "--remote", "origin", "--push");
        if (!created.Succeeded)
        {
            console.Bad($"GitHub would not create the repo \"{repoName}\":");
            console.Line(created.Output.Trim());
            console.Line("    Your work is saved on this computer.");
            return SaveOutcome.SavedOnThisComputerOnly;
        }

        console.Good($"Made a private GitHub repo called {repoName} and pushed to it.");
        return SaveOutcome.Pushed;
    }

    private async Task<bool> EnsureGitKnowsWhoYouAre(string folder, CancellationToken cancel)
    {
        var knownEmail = await Git(folder, cancel, "config", "user.email");
        if (knownEmail.Succeeded && knownEmail.Output.Trim().Length > 0)
        {
            return true;
        }

        var account = await GitHub(folder, cancel, "api", "user", "--jq", "\"\\(.id),\\(.login)\"");
        var idAndLogin = account.Output.Trim().Split(',');

        if (account.Succeeded && idAndLogin.Length == 2)
        {
            await Git(folder, cancel, "config", "user.name", idAndLogin[1]);
            await Git(folder, cancel, "config", "user.email", $"{idAndLogin[0]}+{idAndLogin[1]}@users.noreply.github.com");
            console.Step($"Git did not know who you are. Using your GitHub name ({idAndLogin[1]}) for this folder only.");
            return true;
        }

        console.Bad("Git does not know who you are. Run these two lines once, then run ess g again:");
        console.Line("    git config --global user.name \"Your Name\"");
        console.Line("    git config --global user.email you@example.com");
        return false;
    }

    private async Task WriteIgnoreFileIfMissing(string folder, CancellationToken cancel)
    {
        var ignorePath = Path.Combine(folder, IgnoreFileName);
        if (!File.Exists(ignorePath))
        {
            await File.WriteAllLinesAsync(ignorePath, namesGitShouldIgnore, cancel);
        }
    }

    private Task<ProcessResult> Git(string folder, CancellationToken cancel, params string[] arguments)
    {
        return subprocess.Run("git", arguments, folder, cancel);
    }

    private Task<ProcessResult> GitHub(string folder, CancellationToken cancel, params string[] arguments)
    {
        return subprocess.Run("gh", arguments, folder, cancel);
    }
}
