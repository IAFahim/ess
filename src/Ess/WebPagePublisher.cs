namespace Ess;

internal enum PublishOutcome
{
    NoSimulators,
    UpToDate,
    Published,
    Failed,
}

internal sealed class WebPagePublisher(Subprocess subprocess, EssConsole console, WebPage webPage)
{
    public const string BranchName = "gh-pages";

    private const string BranchRef = "refs/heads/" + BranchName;
    private const string TrackingRef = "refs/remotes/origin/" + BranchName;
    private const string FileMode = "100644";
    private const string PagesEndpoint = "repos/{owner}/{repo}/pages";
    private const string TurnOnPagesJson = """{"build_type":"legacy","source":{"branch":"gh-pages","path":"/"}}""";

    public async Task<PublishOutcome> Publish(string folder, CancellationToken cancel)
    {
        var simulators = await Simulators(folder, cancel);
        if (simulators.Count == 0)
        {
            return PublishOutcome.NoSimulators;
        }

        var tree = await BuildTree(folder, simulators, cancel);
        if (tree is null)
        {
            console.Warn("Could not make the web page. Your work is still saved and pushed.");
            return PublishOutcome.Failed;
        }

        await Git(folder, cancel, "fetch", "-q", "origin", $"+{BranchRef}:{TrackingRef}");
        var parent = await Git(folder, cancel, "rev-parse", "-q", "--verify", TrackingRef);
        var hasParent = parent.Succeeded;
        var outcome = PublishOutcome.UpToDate;

        if (!hasParent || await TreeOf(folder, TrackingRef, cancel) != tree)
        {
            if (!await PushPages(folder, tree, hasParent ? parent.Output.Trim() : null, cancel))
            {
                console.Warn("Could not update the web page. Run ess g again later.");
                return PublishOutcome.Failed;
            }

            outcome = PublishOutcome.Published;
        }

        await TurnOnGitHubPages(folder, outcome, cancel);
        return outcome;
    }

    private async Task<IReadOnlyList<TreeEntry>> Simulators(string folder, CancellationToken cancel)
    {
        var listing = await Git(folder, cancel, "ls-tree", "-r", "-z", "HEAD");
        if (!listing.Succeeded)
        {
            return [];
        }

        return listing.Output
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(TreeEntry.Parse)
            .OfType<TreeEntry>()
            .Where(entry => webPage.IsSimulator(entry.Path))
            .ToList();
    }

    private async Task<string?> BuildTree(string folder, IReadOnlyList<TreeEntry> simulators, CancellationToken cancel)
    {
        var title = Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)));
        var index = await Blob(folder, webPage.Index(title, simulators.Select(entry => entry.Path)), cancel);
        var noJekyll = await Blob(folder, "", cancel);
        if (index is null || noJekyll is null)
        {
            return null;
        }

        var entries = simulators
            .Append(new TreeEntry(index, WebPage.IndexFileName))
            .Append(new TreeEntry(noJekyll, WebPage.NoJekyllFileName))
            .Select(entry => $"{FileMode} {entry.Blob}\t{entry.Path}\0");

        var indexFile = Path.Combine(Path.GetTempPath(), $"ess-pages-{Guid.NewGuid():N}.index");
        var temporaryIndex = new Dictionary<string, string> { ["GIT_INDEX_FILE"] = indexFile };
        try
        {
            var added = await subprocess.Run("git", ["update-index", "-z", "--add", "--index-info"], folder, cancel, temporaryIndex, string.Concat(entries));
            if (!added.Succeeded)
            {
                return null;
            }

            var written = await subprocess.Run("git", ["write-tree"], folder, cancel, temporaryIndex);
            return written.Succeeded ? written.Output.Trim() : null;
        }
        finally
        {
            File.Delete(indexFile);
        }
    }

    private async Task<string?> Blob(string folder, string content, CancellationToken cancel)
    {
        var stored = await subprocess.Run("git", ["hash-object", "-w", "--stdin"], folder, cancel, standardInput: content);
        return stored.Succeeded ? stored.Output.Trim() : null;
    }

    private async Task<string?> TreeOf(string folder, string reference, CancellationToken cancel)
    {
        var tree = await Git(folder, cancel, "rev-parse", "-q", "--verify", reference + "^{tree}");
        return tree.Succeeded ? tree.Output.Trim() : null;
    }

    private async Task<bool> PushPages(string folder, string tree, string? parent, CancellationToken cancel)
    {
        string[] parentArguments = parent is null ? [] : ["-p", parent];
        var commit = await Git(folder, cancel, ["commit-tree", tree, .. parentArguments, "-m", "ess: web page"]);
        if (!commit.Succeeded)
        {
            return false;
        }

        var page = commit.Output.Trim();
        var pushed = await Git(folder, cancel, "push", "-q", "origin", $"{page}:{BranchRef}");
        if (!pushed.Succeeded)
        {
            return false;
        }

        await Git(folder, cancel, "update-ref", TrackingRef, page);
        return true;
    }

    private async Task TurnOnGitHubPages(string folder, PublishOutcome outcome, CancellationToken cancel)
    {
        var origin = await Git(folder, cancel, "remote", "get-url", "origin");
        if (!origin.Output.Contains("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var page = await GitHub(folder, cancel, null, "api", PagesEndpoint, "--jq", ".html_url");
        if (page.ProgramMissing)
        {
            console.Warn("To turn on the web page, install gh from https://cli.github.com then run ess g again.");
            return;
        }

        if (page.Succeeded)
        {
            var updating = outcome == PublishOutcome.Published ? " (it updates in a minute)" : "";
            console.Good($"Web page: {page.Output.Trim()}{updating}");
            return;
        }

        var turnedOn = await GitHub(folder, cancel, TurnOnPagesJson, "api", "-X", "POST", PagesEndpoint, "--input", "-", "--jq", ".html_url");
        if (!turnedOn.Succeeded)
        {
            console.Warn("GitHub would not turn on the web page:");
            console.Line(turnedOn.Output.Trim());
            return;
        }

        console.Good($"Made a web page. It is ready in a minute or two: {turnedOn.Output.Trim()}");
        console.Line("    Anyone with the link can open it. It has only the simulators, not your code.");
    }

    private Task<ProcessResult> Git(string folder, CancellationToken cancel, params string[] arguments) =>
        subprocess.Run("git", arguments, folder, cancel);

    private Task<ProcessResult> GitHub(string folder, CancellationToken cancel, string? standardInput, params string[] arguments) =>
        subprocess.Run("gh", arguments, folder, cancel, standardInput: standardInput);

    private sealed record TreeEntry(string Blob, string Path)
    {
        public static TreeEntry? Parse(string line)
        {
            var tab = line.IndexOf('\t');
            string[] fields = tab < 0 ? [] : line[..tab].Split(' ');
            var isFile = fields.Length == 3 && fields[1] == "blob";
            return isFile ? new TreeEntry(fields[2], line[(tab + 1)..]) : null;
        }
    }
}
