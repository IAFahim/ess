namespace Ess;

internal sealed class EssHome
{
    private const string OverrideVariable = "ESS_HOME";

    public string Root { get; } = Environment.GetEnvironmentVariable(OverrideVariable)
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ess");

    public string DownloadDirectory => Path.Combine(Root, "downloads");

    public string ToolDirectory(string stateSmithVersion) => Path.Combine(Root, $"statesmith-{stateSmithVersion}");
}
