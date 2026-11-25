namespace Ess;

internal sealed class DotnetRunner(Subprocess subprocess, DotnetLocator dotnet, EssConsole console)
{
    private readonly string[] runArguments = ["run", "--nologo"];

    public async Task<bool> Run(string projectFolder, string howToRunIt, CancellationToken cancel)
    {
        console.Line();
        console.Step($"Running it:  {howToRunIt}   (first time takes a few seconds)");

        var install = await dotnet.Find(cancel);
        if (install is null)
        {
            console.Line();
            console.Warn("I could not find a .NET 10 SDK, so I could not run it. Get it from https://dotnet.microsoft.com/download");
            return false;
        }

        var result = await subprocess.Run(install.Executable, runArguments, projectFolder, cancel, install.Environment);
        console.Line();

        console.Line(result.Output.Trim());

        if (!result.Succeeded)
        {
            console.Warn("dotnet run did not work. The files are made. Run it yourself to see the full error.");
        }

        return result.Succeeded;
    }
}
