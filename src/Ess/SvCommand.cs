namespace Ess;

internal sealed class SvCommand(SvRepo repo, Subprocess subprocess, EssConsole console)
{
    public const string CloneFolderName = "sv";
    public const string DemoFolderName = "demo-sv";

    private const string HelloProject = "samples/Sv.Hello";
    private const string HelloExpectedFile = "expected.txt";

    private readonly string[] helloArguments = ["run", "-c", "Release", "--nologo", "--project", HelloProject];

    public async Task<int> CreateHere(string folder, CancellationToken cancel)
    {
        var cloneFolder = Path.Combine(folder, CloneFolderName);
        if (Directory.Exists(cloneFolder))
        {
            console.Warn("sv is already here. I did not touch it. To get the latest:  ess u sv");
            return 1;
        }

        if (!await repo.Clone(folder, CloneFolderName, cancel))
        {
            return 1;
        }

        console.Line();
        console.Step("Try its first example:  cd sv  then  dotnet run -c Release --project samples/Sv.Hello");
        console.Step("Use it in your own project: sv/README.md, chapter 5");
        console.Step("Get the latest later:  ess u sv");
        return 0;
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

        if (Directory.Exists(demoFolder))
        {
            if (!await repo.IsSvClone(demoFolder, cancel))
            {
                console.Warn($"{DemoFolderName} is already here and it is not sv. I did not touch it.");
                return 1;
            }

            console.Step($"{DemoFolderName} is here. Getting the latest sv and resetting it.");
            if (!await repo.Update(demoFolder, throwAwayLocalChanges: true, cancel))
            {
                return 1;
            }
        }
        else if (!await repo.Clone(folder, DemoFolderName, cancel))
        {
            return 1;
        }

        await RunHello(demoFolder, cancel);

        console.Line();
        console.Step($"Look around:  cd {DemoFolderName}  then read README.md");
        console.Step("Run  ess dsv  again any time to get the latest sv and reset this.");
        return 0;
    }

    private async Task RunHello(string demoFolder, CancellationToken cancel)
    {
        console.Line();
        console.Step("Running sv's first example (the first build takes a minute).");

        var result = await subprocess.Run("dotnet", helloArguments, demoFolder, cancel);
        console.Line();

        if (result.ProgramMissing)
        {
            console.Warn("dotnet is not installed. Get the .NET 10 SDK from https://dotnet.microsoft.com/download");
            return;
        }

        console.Line(result.Output.Trim());

        if (!result.Succeeded)
        {
            console.Warn("The example did not run. sv is cloned. Run it yourself to see the full error.");
            return;
        }

        var expectedPath = Path.Combine(demoFolder, HelloProject, HelloExpectedFile);
        var expected = File.Exists(expectedPath) ? (await File.ReadAllTextAsync(expectedPath, cancel)).Trim() : null;

        if (expected is null)
        {
            return;
        }

        console.Line();
        if (Normalised(result.Output).EndsWith(Normalised(expected), StringComparison.Ordinal))
        {
            console.Good("That is exactly what sv says it should print.");
        }
        else
        {
            console.Warn("That is not what sv's expected.txt says. Something differs on this computer.");
        }
    }

    private static string Normalised(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
}
