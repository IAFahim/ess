namespace Ess;

internal sealed class GitCommand(GitSaver git)
{
    private const string CommitMessage = "ess: save";

    public async Task<int> Run(string folder, CancellationToken cancel)
    {
        var outcome = await git.Save(folder, CommitMessage, createGitHubRepoWhenMissing: true, cancel);
        return outcome == SaveOutcome.NeedsHelp ? 1 : 0;
    }
}
