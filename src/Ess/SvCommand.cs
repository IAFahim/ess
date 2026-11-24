namespace Ess;

internal sealed class SvCommand(
    SvRepo repo,
    DemoFiles files,
    CodeGenerator generator,
    Subprocess subprocess,
    MachineNames machineNames,
    EssConsole console)
{
    public const string SvFolder = "lib/sv";
    public const string DemoFolderName = "demo-sv";

    private const string DemoMarkerFileName = ".ess-demo";
    private const string ResourceFolder = "sv";
    private const string SamplePackFolder = "samples/Sv.Hello/pack";
    private const string NamePlaceholder = "{{Name}}";
    private const string WoundedState = "StateSmith: WOUNDED";
    private const string HealthyState = "StateSmith: HEALTHY";

    private readonly string[] runArguments = ["run", "-c", "Release", "--nologo"];

    private readonly Dictionary<string, string> renamedOnWrite = new()
    {
        ["Host.csproj"] = NamePlaceholder + ".csproj",
        ["gitignore.txt"] = ".gitignore",
    };

    public async Task<int> CreateHere(string folder, CancellationToken cancel)
    {
        var alreadyHere = Directory.Exists(Path.Combine(folder, SvFolder))
            || Directory.EnumerateFiles(folder, "*.csproj").Any()
            || File.Exists(Path.Combine(folder, "Program.cs"));

        if (alreadyHere)
        {
            console.Warn("This folder already has a project or sv. I did not touch it. To get the latest sv:  ess u sv");
            return 1;
        }

        var name = machineNames.ToIdentifier(Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)));
        return await MakeProject(folder, name, refresh: false, nestingIsFine: false, cancel) ? 0 : 1;
    }

    public async Task<int> Update(string folder, CancellationToken cancel)
    {
        var inside = Path.Combine(folder, SvFolder);

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

        var name = machineNames.ToIdentifier(DemoFolderName);
        if (!await MakeProject(demoFolder, name, refresh, nestingIsFine: true, cancel))
        {
            return 1;
        }

        console.Line();
        console.Step("Run  ess d sv  again any time to get the latest sv and reset this.");
        return 0;
    }

    private async Task<bool> MakeProject(string projectFolder, string name, bool refresh, bool nestingIsFine, CancellationToken cancel)
    {
        var svFolder = Path.Combine(projectFolder, SvFolder);

        var gotSv = refresh && Directory.Exists(svFolder)
            ? await repo.Update(svFolder, throwAwayLocalChanges: true, cancel)
            : await repo.AddAsSubmodule(projectFolder, SvFolder, allowInsideAnotherRepo: nestingIsFine, cancel);

        if (!gotSv)
        {
            return false;
        }

        var written = await WriteHostFiles(projectFolder, name, cancel);

        if (!CopyPackFromSv(svFolder, projectFolder))
        {
            console.Bad($"This sv has no {SamplePackFolder}, so I have no pack to start from.");
            return false;
        }

        console.Good($"made the project in {projectFolder}  ({written.Count} files and sv's pack)");

        if (!await generator.Make(projectFolder, cancel))
        {
            return false;
        }

        await Run(projectFolder, cancel);
        return true;
    }

    private async Task<IReadOnlyList<string>> WriteHostFiles(string projectFolder, string name, CancellationToken cancel)
    {
        var written = new List<string>();

        foreach (var (fileName, content) in await files.ReadAll(ResourceFolder, cancel))
        {
            var targetName = renamedOnWrite.GetValueOrDefault(fileName, fileName).Replace(NamePlaceholder, name, StringComparison.Ordinal);
            await File.WriteAllTextAsync(Path.Combine(projectFolder, targetName), content.Replace(NamePlaceholder, name, StringComparison.Ordinal), cancel);
            written.Add(targetName);
        }

        return written;
    }

    private static bool CopyPackFromSv(string svFolder, string projectFolder)
    {
        var samplePack = Path.Combine(svFolder, SamplePackFolder);
        if (!Directory.Exists(samplePack))
        {
            return false;
        }

        var projectPack = Path.Combine(projectFolder, "pack");
        Directory.CreateDirectory(projectPack);

        foreach (var packFile in Directory.EnumerateFiles(samplePack, "*.sv.json"))
        {
            File.Copy(packFile, Path.Combine(projectPack, Path.GetFileName(packFile)), overwrite: true);
        }

        return true;
    }

    private async Task Run(string projectFolder, CancellationToken cancel)
    {
        console.Line();
        console.Step("Running it (the first build takes a minute):  dotnet run -c Release");

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
