namespace Ess;

internal sealed class SvCommand(
    SvRepo repo,
    DemoFiles files,
    CodeGenerator generator,
    Subprocess subprocess,
    EssConsole console)
{
    public const string CloneFolderName = "sv";
    public const string AppFolderName = "app";
    public const string DemoFolderName = "demo-sv";

    private const string DemoMarkerFileName = ".ess-demo";
    private const string ResourceFolder = "sv";
    private const string SamplePackFolder = "samples/Sv.Hello/pack";
    private const string WoundedState = "StateSmith: WOUNDED";
    private const string HealthyState = "StateSmith: HEALTHY";

    private readonly string[] runArguments = ["run", "-c", "Release", "--nologo", "--project", AppFolderName];

    public async Task<int> CreateHere(string folder, CancellationToken cancel)
    {
        var existing = new[] { CloneFolderName, AppFolderName }.Where(name => Directory.Exists(Path.Combine(folder, name))).ToList();
        if (existing.Count > 0)
        {
            console.Warn($"{string.Join(" and ", existing)} already here. I did not touch it. To get the latest sv:  ess u sv");
            return 1;
        }

        return await MakeProject(folder, refreshSv: false, cancel) ? 0 : 1;
    }

    public async Task<int> Update(string folder, CancellationToken cancel)
    {
        var inside = Path.Combine(folder, CloneFolderName);

        if (await repo.IsSvClone(inside, cancel))
        {
            return await repo.Update(inside, throwAwayLocalChanges: false, cancel) ? 0 : 1;
        }

        if (await repo.IsSvClone(folder, cancel))
        {
            return await repo.Update(folder, throwAwayLocalChanges: false, cancel) ? 0 : 1;
        }

        console.Warn("There is no sv here. To get it:  ess n sv");
        return 1;
    }

    public async Task<int> Demo(string folder, CancellationToken cancel)
    {
        var demoFolder = Path.Combine(folder, DemoFolderName);
        var markerPath = Path.Combine(demoFolder, DemoMarkerFileName);
        var refresh = Directory.Exists(demoFolder);

        if (refresh && !File.Exists(markerPath))
        {
            console.Warn($"{DemoFolderName} is already here and I did not make it. I did not touch it.");
            return 1;
        }

        if (refresh)
        {
            console.Step($"{DemoFolderName} is here. Getting the latest sv and resetting the demo.");
        }
        else
        {
            Directory.CreateDirectory(demoFolder);
            await File.WriteAllTextAsync(markerPath, "made by ess d sv. ess d sv again resets this folder.\n", cancel);
        }

        if (!await MakeProject(demoFolder, refreshSv: refresh, cancel))
        {
            return 1;
        }

        console.Line();
        console.Step("Run  ess d sv  again any time to get the latest sv and reset this.");
        return 0;
    }

    private async Task<bool> MakeProject(string projectFolder, bool refreshSv, CancellationToken cancel)
    {
        var cloneFolder = Path.Combine(projectFolder, CloneFolderName);
        var appFolder = Path.Combine(projectFolder, AppFolderName);

        var gotSv = Directory.Exists(cloneFolder) && refreshSv
            ? await repo.Update(cloneFolder, throwAwayLocalChanges: true, cancel)
            : await repo.Clone(projectFolder, CloneFolderName, cancel);

        if (!gotSv)
        {
            return false;
        }

        var written = await files.WriteTo(appFolder, ResourceFolder, cancel);
        var packCopied = CopyPackFromSv(cloneFolder, appFolder);
        if (!packCopied)
        {
            console.Bad($"This sv has no {SamplePackFolder}, so I have no pack to start from.");
            return false;
        }

        console.Good($"made {appFolder}  ({written.Count} files and sv's pack)");

        if (!await generator.Make(appFolder, cancel))
        {
            return false;
        }

        await Run(projectFolder, cancel);
        return true;
    }

    private static bool CopyPackFromSv(string cloneFolder, string appFolder)
    {
        var samplePack = Path.Combine(cloneFolder, SamplePackFolder);
        if (!Directory.Exists(samplePack))
        {
            return false;
        }

        var appPack = Path.Combine(appFolder, "pack");
        Directory.CreateDirectory(appPack);

        foreach (var packFile in Directory.EnumerateFiles(samplePack, "*.sv.json"))
        {
            File.Copy(packFile, Path.Combine(appPack, Path.GetFileName(packFile)), overwrite: true);
        }

        return true;
    }

    private async Task Run(string projectFolder, CancellationToken cancel)
    {
        console.Line();
        console.Step("Running it (the first build takes a minute):  dotnet run -c Release --project app");

        var result = await subprocess.Run("dotnet", runArguments, projectFolder, cancel);
        console.Line();

        if (result.ProgramMissing)
        {
            console.Warn("dotnet is not installed. Get the .NET 10 SDK from https://dotnet.microsoft.com/download");
            return;
        }

        console.Line(result.Output.Trim());
        console.Line();

        if (!result.Succeeded)
        {
            console.Warn("It did not run. Everything is made. Run it yourself to see the full error.");
            return;
        }

        var output = result.Output;
        var woundedAt = output.IndexOf(WoundedState, StringComparison.Ordinal);
        var healthyAgainAt = woundedAt < 0 ? -1 : output.IndexOf(HealthyState, woundedAt, StringComparison.Ordinal);

        if (healthyAgainAt >= 0)
        {
            console.Good("sv sent its low-health event and the StateSmith machine followed: Healthy, Wounded, Healthy.");
        }
        else
        {
            console.Warn("It ran, but the StateSmith machine did not follow sv as expected.");
        }
    }
}
