namespace Ess;

internal sealed class UnityCommand(
    UnityPackage package,
    StarterDiagram starter,
    MachineNames machineNames,
    DotnetFolder dotnetFolder,
    CodeGenerator generator,
    UserFolders userFolders,
    EssConsole console)
{
    public async Task<int> Run(IReadOnlyList<string> arguments, string folder, CancellationToken cancel)
    {
        if (package.UnityProjectAround(folder) is { } unityProject)
        {
            console.Bad($"This folder is inside a Unity project: {unityProject}");
            console.Line("    Make the package in its own folder, outside Unity. Then add it from Unity.");
            return 1;
        }

        if (IsHome(folder))
        {
            console.Bad("This is your home folder. Make a new folder, go into it, and run  ess unity  there.");
            return 1;
        }

        if (package.IsPackage(folder))
        {
            console.Warn($"This folder is already a package. I did not touch {UnityPackage.ManifestFileName}.");
            PrintSteps(folder);
            return 0;
        }

        var machineName = arguments.Count > 0
            ? machineNames.ToIdentifier(arguments[0])
            : machineNames.DefaultForFolder(folder);

        await Write(folder, UnityPackage.ManifestFileName, package.Manifest(folder), $"  ({package.PackageName(folder)})", cancel);
        await Write(folder, package.AssemblyFileName(folder), package.AssemblyDefinition(folder), "", cancel);
        await Write(folder, UnityPackage.ReadmeFileName, package.Readme(folder), "", cancel);
        await Write(folder, machineName + ".plantuml", starter.Render(machineName, inPackage: true), "", cancel);

        Directory.CreateDirectory(Path.Combine(folder, DotnetFolder.FolderName));
        await Write(folder, Path.Combine(DotnetFolder.FolderName, dotnetFolder.ProjectFileName(folder)), dotnetFolder.Project(), "  (runs the machines without Unity)", cancel);
        await Write(folder, Path.Combine(DotnetFolder.FolderName, DotnetFolder.ProgramFileName), dotnetFolder.Program(machineName), "", cancel);

        if (!await generator.Make(folder, cancel))
        {
            return 1;
        }

        PrintSteps(folder);
        return 0;
    }

    private async Task Write(string folder, string fileName, string content, string note, CancellationToken cancel)
    {
        var path = Path.Combine(folder, fileName);

        if (File.Exists(path))
        {
            console.Warn($"{fileName} is already here. I did not touch it.");
            return;
        }

        await File.WriteAllTextAsync(path, content, cancel);
        console.Good($"made {fileName}{note}");
    }

    private void PrintSteps(string folder)
    {
        console.Line();
        console.Line("Add it to Unity (once):");
        console.Step("1. Window > Package Management > Package Manager");
        console.Step("2. Press +  then  Install package from disk...");
        console.Step($"3. Pick  {Path.Combine(folder, UnityPackage.ManifestFileName)}");
        console.Line();
        console.Line("Then, here (not in Unity):");
        console.Step("ess w   edit the diagram, Unity gets the new code on every save");
        console.Step("ess r   make the code and run it in .NET, no Unity needed");
        console.Step("ess g   save to GitHub");
    }

    private bool IsHome(string folder)
    {
        var home = Path.TrimEndingDirectorySeparator(Path.GetFullPath(userFolders.Home));
        return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)), home, StringComparison.Ordinal);
    }
}
