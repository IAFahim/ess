namespace Ess;

internal sealed class DemoCommand(
    DemoCatalog demos,
    DemoSelector selector,
    DemoFiles files,
    CodeGenerator generator,
    Subprocess subprocess,
    DotnetLocator dotnet,
    SvCommand sv,
    EssKeys keys,
    EssConsole console)
{
    private const char QuitKey = 'q';

    private readonly string[] dotnetRunArguments = ["run", "--nologo"];

    public async Task<int> Run(IReadOnlyList<string> arguments, string folder, bool chooseAfterList, CancellationToken cancel)
    {
        var which = arguments.Count > 0 ? arguments[0] : await ListThenAsk(chooseAfterList, cancel);

        if (which is null)
        {
            return 0;
        }

        var demo = selector.Find(which);
        if (demo is null)
        {
            console.UsageError($"There is no demo \"{which}\".");
            console.UsageHint("See the list:  ess d");
            return 1;
        }

        if (demo.IsSvEngine)
        {
            return await sv.Demo(folder, cancel);
        }

        return await MakeDemo(demo, folder, cancel);
    }

    private async Task<string?> ListThenAsk(bool chooseAfterList, CancellationToken cancel)
    {
        foreach (var line in demos.ListLines())
        {
            console.Line(line);
        }

        if (!chooseAfterList)
        {
            return null;
        }

        console.Line();
        console.Line($"Press a number to make it  ({QuitKey} to quit):");

        var pressed = await keys.ReadAsync(cancel);
        return pressed == QuitKey ? null : pressed.ToString();
    }

    private async Task<int> MakeDemo(DemoInfo demo, string folder, CancellationToken cancel)
    {
        var demoFolder = Path.Combine(folder, demo.FolderName);
        if (Directory.Exists(demoFolder))
        {
            console.Warn($"{demo.FolderName} is already here. I did not touch it.");
            console.Line($"    Delete it to start fresh, or:  cd {demo.FolderName}");
            return 1;
        }

        console.Step($"Demo {demo.Number}, {demo.Name}: {demo.Title}");

        var written = await files.WriteTo(demoFolder, demo.ResourceFolder, cancel);
        console.Good($"made folder {demoFolder}  ({written.Count} files)");

        if (!await generator.Make(demoFolder, cancel))
        {
            return 1;
        }

        if (demo.RunsWithDotnet)
        {
            await RunWithDotnet(demoFolder, cancel);
        }

        console.Line();
        foreach (var nextStep in demo.NextSteps)
        {
            console.Step(nextStep);
        }

        console.Step("More demos:  ess d");
        return 0;
    }

    private async Task RunWithDotnet(string demoFolder, CancellationToken cancel)
    {
        console.Line();
        console.Step($"Running it:  cd {demoFolder} && dotnet run   (first time takes a few seconds)");

        var install = await dotnet.Find(cancel);
        if (install is null)
        {
            console.Line();
            console.Warn("I could not find a .NET 10 SDK, so I could not run it. Get it from https://dotnet.microsoft.com/download");
            return;
        }

        var result = await subprocess.Run(install.Executable, dotnetRunArguments, demoFolder, cancel, install.Environment);
        console.Line();

        console.Line(result.Output.Trim());

        if (!result.Succeeded)
        {
            console.Warn("dotnet run did not work. The demo files are made. Run it yourself to see the full error.");
        }
    }
}
