namespace Ess;

internal sealed record DotnetInstall(string Executable, string? Root)
{
    public IReadOnlyDictionary<string, string>? Environment =>
        Root is null ? null : new Dictionary<string, string> { ["DOTNET_ROOT"] = Root };
}

internal sealed class DotnetLocator(Subprocess subprocess, SdkList sdkList)
{
    private const int RequiredMajor = 10;

    private DotnetInstall? found;
    private bool searched;

    public async Task<DotnetInstall?> Find(CancellationToken cancel)
    {
        if (searched)
        {
            return found;
        }

        var workingFolder = Directory.GetCurrentDirectory();

        foreach (var candidate in Candidates())
        {
            var result = await subprocess.Run(candidate.Executable, ["--list-sdks"], workingFolder, cancel, candidate.Environment);

            if (result.Succeeded && sdkList.HasAtLeast(result.Output, RequiredMajor))
            {
                found = candidate;
                break;
            }
        }

        searched = true;
        return found;
    }

    private static IEnumerable<DotnetInstall> Candidates()
    {
        var fileName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        var home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

        yield return new DotnetInstall("dotnet", null);

        var configuredRoot = System.Environment.GetEnvironmentVariable("DOTNET_ROOT");
        var roots = new List<string?>
        {
            configuredRoot,
            Path.Combine(home, ".dotnet"),
            "/usr/share/dotnet",
            "/usr/lib/dotnet",
            "/usr/local/share/dotnet",
            "/opt/dotnet",
            System.Environment.GetEnvironmentVariable("ProgramFiles") is { } programFiles ? Path.Combine(programFiles, "dotnet") : null,
            System.Environment.GetEnvironmentVariable("LOCALAPPDATA") is { } localAppData ? Path.Combine(localAppData, "Microsoft", "dotnet") : null,
        };

        var miseInstalls = Path.Combine(home, ".local", "share", "mise", "installs", "dotnet");
        if (Directory.Exists(miseInstalls))
        {
            roots.AddRange(Directory.EnumerateDirectories(miseInstalls).OrderDescending());
        }

        foreach (var root in roots.Where(root => !string.IsNullOrEmpty(root)).Distinct())
        {
            var executable = Path.Combine(root!, fileName);
            if (File.Exists(executable))
            {
                yield return new DotnetInstall(executable, root);
            }
        }
    }
}
