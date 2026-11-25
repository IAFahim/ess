using System.Text;

namespace Ess;

internal sealed class UnityPackage(MachineNames machineNames)
{
    public const string ManifestFileName = "package.json";
    public const string ReadmeFileName = "README.md";

    private const string NamePrefix = "com.ess.";
    private const string NameWhenNothingUsable = "machines";
    private const string AssemblyPrefix = "Ess.";
    private const string UnityProjectMarker = "ProjectSettings/ProjectVersion.txt";

    private const string ManifestTemplate = """
        {
          "name": "{package}",
          "version": "0.1.0",
          "displayName": "{display}",
          "description": "State machines made by ess from the .plantuml files in this folder. Edit them outside Unity; ess w makes the code.",
          "unity": "7000.0",
          "dependencies": {
            "com.unity.entities": "7.0.0"
          }
        }
        """;

    private const string AssemblyTemplate = """
        {
          "name": "{assembly}",
          "rootNamespace": "Machines",
          "references": [
            "Unity.Burst",
            "Unity.Collections",
            "Unity.Entities",
            "Unity.Mathematics"
          ],
          "autoReferenced": true
        }
        """;

    private const string ReadmeTemplate = """
        # {display}

        State machines for Unity ECS, made by ess.
        The diagrams live here, outside Unity. Unity only gets the made code.

        ## Add it to Unity (once)

        1. Window > Package Management > Package Manager
        2. Press +, then "Install package from disk..."
        3. Pick the package.json in this folder.

        Unity now shows "{display}" in the Package Manager.

        ## Every day

            ess w       make the code every time you save a diagram
            ess o       open the diagrams in omaspec
            ess n door  add another machine called Door
            ess g       save to GitHub

        ## Use a machine

        Each diagram makes a struct in the namespace Machines. It is an ECS component.
        Scripts in Assets see it. Your own .asmdef needs a reference to {assembly}.

        - Pass the machine by ref. A copy changes only the copy.
        - A new (zero) machine has stateId ROOT. Call Start() once when you see that.
        - Diagram actions must be Burst safe: numbers and bools in vars, no strings, no lists.

        A full example with a Burst job and tests: ess d 3
        """;

    public bool IsPackage(string folder) => File.Exists(Path.Combine(folder, ManifestFileName));

    public string? UnityProjectAround(string folder)
    {
        for (var current = new DirectoryInfo(folder); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, UnityProjectMarker)))
            {
                return current.FullName;
            }
        }

        return null;
    }

    public string PackageName(string folder)
    {
        var name = new StringBuilder();
        var pendingDash = false;

        foreach (var character in FolderName(folder))
        {
            if (!char.IsAsciiLetterOrDigit(character))
            {
                pendingDash = name.Length > 0;
                continue;
            }

            if (pendingDash)
            {
                name.Append('-');
                pendingDash = false;
            }

            name.Append(char.ToLowerInvariant(character));
        }

        return NamePrefix + (name.Length == 0 ? NameWhenNothingUsable : name.ToString());
    }

    public string DisplayName(string folder) => machineNames.ToIdentifier(FolderName(folder)) + " machines";

    public string AssemblyName(string folder) => AssemblyPrefix + machineNames.ToIdentifier(FolderName(folder));

    public string AssemblyFileName(string folder) => AssemblyName(folder) + ".asmdef";

    public string Manifest(string folder) => Fill(ManifestTemplate, folder);

    public string AssemblyDefinition(string folder) => Fill(AssemblyTemplate, folder);

    public string Readme(string folder) => Fill(ReadmeTemplate, folder);

    private string Fill(string template, string folder)
    {
        return template
            .Replace("{package}", PackageName(folder), StringComparison.Ordinal)
            .Replace("{display}", DisplayName(folder), StringComparison.Ordinal)
            .Replace("{assembly}", AssemblyName(folder), StringComparison.Ordinal) + "\n";
    }

    private static string FolderName(string folder) => Path.GetFileName(Path.TrimEndingDirectorySeparator(folder));
}
