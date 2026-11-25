namespace Ess;

internal sealed class NewCommand(
    CodeGenerator generator,
    StateSmithTool tool,
    StarterDiagram starter,
    MachineNames machineNames,
    UnityPackage unityPackage,
    SvCommand sv,
    EssConsole console)
{
    public async Task<int> Run(string[] extraArguments, string folder, CancellationToken cancel)
    {
        if (extraArguments.Length > 0 && extraArguments[0].Trim().Equals("sv", StringComparison.OrdinalIgnoreCase))
        {
            return await sv.CreateHere(folder, cancel);
        }

        var machineName = extraArguments.Length > 0
            ? machineNames.ToIdentifier(extraArguments[0])
            : machineNames.DefaultForFolder(folder);

        var diagramFileName = machineName + ".plantuml";
        var diagramPath = Path.Combine(folder, diagramFileName);

        if (File.Exists(diagramPath))
        {
            console.Warn($"{diagramFileName} is already here. I did not touch it.");
        }
        else
        {
            await File.WriteAllTextAsync(diagramPath, starter.Render(machineName, inPackage: unityPackage.IsPackage(folder)), cancel);
            console.Good($"made {diagramFileName}");
        }

        if (await tool.EnsureInstalled(cancel) is null)
        {
            return 1;
        }

        if (!await generator.Make(folder, cancel))
        {
            return 1;
        }

        console.Line();
        console.Step("Next:  ess w   watch, and make the code on every save");
        console.Step("       ess g   save to GitHub");
        return 0;
    }
}
