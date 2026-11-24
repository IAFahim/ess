namespace Ess;

internal sealed class DemoCommand(
    DemoCatalog demos,
    DemoRequest request,
    DemoFiles files,
    CodeGenerator generator,
    Subprocess subprocess,
    EssConsole console)
{
    private readonly string[] dotnetRunArguments = ["run", "--nologo"];

    public async Task<int> Run(string typed, IReadOnlyList<string> extraArguments, string folder, CancellationToken cancel)
    {
        var level = request.LevelFrom(typed, extraArguments);
        var demo = level is null ? null : demos.ForLevel(level.Value);

        if (demo is null)
        {
            console.Bad($"There is no demo \"{typed}\". The demos are:");
            ListDemos();
            return 1;
        }

        var demoFolder = Path.Combine(folder, demo.FolderName);
        if (Directory.Exists(demoFolder))
        {
            console.Warn($"{demo.FolderName} is already here. I did not touch it.");
            console.Line($"    Delete it to start fresh, or:  cd {demo.FolderName}");
            return 1;
        }

        console.Step($"Demo {(demo.Level == 0 ? "d" : "d" + demo.Level)}: {demo.Title}");

        var written = await files.WriteTo(demoFolder, demo.ResourceFolder, cancel);
        console.Good($"made folder {demoFolder}  ({written.Count} files)");

        if (!await generator.Make(demoFolder, cancel))
        {
            return 1;
        }

        if (demo.RunsWithDotnet)
        {
            await RunWithDotnet(demo, demoFolder, cancel);
        }

        console.Line();
        foreach (var nextStep in demo.NextSteps)
        {
            console.Step(nextStep);
        }

        console.Step("More demos:  " + demos.Listing);
        return 0;
    }

    private async Task RunWithDotnet(DemoInfo demo, string demoFolder, CancellationToken cancel)
    {
        console.Line();
        console.Step($"Running it:  cd {demoFolder} && dotnet run   (first time takes a few seconds)");

        var result = await subprocess.Run("dotnet", dotnetRunArguments, demoFolder, cancel);
        console.Line();

        if (result.ProgramMissing)
        {
            console.Warn("dotnet is not installed, so I could not run it. Get the .NET SDK from https://dotnet.microsoft.com/download");
            return;
        }

        console.Line(result.Output.Trim());

        if (!result.Succeeded)
        {
            console.Warn("dotnet run did not work. The demo files are made. Run it yourself to see the full error.");
        }
    }

    private void ListDemos()
    {
        foreach (var demo in demos.All)
        {
            console.Line($"  {(demo.Level == 0 ? "d" : "d" + demo.Level),-3} {demo.Title}");
        }
    }
}
