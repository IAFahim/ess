using System.Text.RegularExpressions;

namespace Ess;

internal readonly record struct MachineType(string? Namespace, string Name);

internal sealed partial class UnityComponentFile
{
    public const string FolderName = "Unity";

    private const string Note = "// Made by ess on every build. This makes {name} a Unity ECS component, so {name}.cs stays plain C#.";

    public MachineType? Read(string structText)
    {
        var machine = MachineDeclaration().Match(structText);
        if (!machine.Success)
        {
            return null;
        }

        var space = NamespaceDeclaration().Match(structText);
        return new MachineType(space.Success ? space.Groups["name"].Value : null, machine.Groups["name"].Value);
    }

    public string FileName(string machineName) => machineName + ".Component.cs";

    public string RelativePath(string machineName) => FolderName + "/" + FileName(machineName);

    public string Text(MachineType machine)
    {
        var note = Note.Replace("{name}", machine.Name, StringComparison.Ordinal);
        var declaration = $"public partial struct {machine.Name} : Unity.Entities.IComponentData";

        if (machine.Namespace is null)
        {
            return $"{note}\n\n{declaration}\n{{\n}}\n";
        }

        return $"{note}\n\nnamespace {machine.Namespace}\n{{\n    {declaration}\n    {{\n    }}\n}}\n";
    }

    [GeneratedRegex(@"^[ \t]*public partial struct (?<name>\w+)", RegexOptions.Multiline)]
    private static partial Regex MachineDeclaration();

    [GeneratedRegex(@"^namespace[ \t]+(?<name>[\w.]+)[ \t]*\r?$", RegexOptions.Multiline)]
    private static partial Regex NamespaceDeclaration();
}
