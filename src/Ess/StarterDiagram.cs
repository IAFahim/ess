namespace Ess;

internal sealed class StarterDiagram
{
    private const string Template = """
        @startuml {name}

        ' ess starter. Edit this file, save it, and ess makes the code again.
        ' To handle an event and stay in the same state, write it with no arrow:
        '   On : TICK / count += 1;
        {structNote}

        [*] --> Off

        Off : enter / count = 0;
        Off --> On : TOGGLE

        On : enter / count += 1;
        On --> Off : TOGGLE

        /'! $CONFIG : toml
        [RenderConfig]
        AutoExpandedVars = "public int count;"
        [RenderConfig.CSharp]
        NameSpace = "Machines"
        {baseList}
        [SmRunnerSettings]
        transpilerId = "CSharp"
        [SmRunnerSettings.smDesignDescriber]
        enabled = true
        [SmRunnerSettings.smDesignDescriber.outputSections]
        beforeTransformations = false
        afterTransformations = true
        '/
        @enduml
        """;

    private const string PlainStructNote = "' ess makes the code a struct. For Unity ECS, remove the # before BaseList below.";
    private const string PackageNote = "' ess makes the code a plain struct. In Unity, the file ess makes in Unity/ turns it into an ECS component.";
    private const string BaseList = "BaseList = \"Unity.Entities.IComponentData\"";

    public string Render(string machineName, bool inPackage = false)
    {
        return Template
            .Replace("{name}", machineName, StringComparison.Ordinal)
            .Replace("{structNote}", inPackage ? PackageNote : PlainStructNote, StringComparison.Ordinal)
            .Replace("{baseList}\n", inPackage ? "" : "# " + BaseList + "\n", StringComparison.Ordinal) + "\n";
    }
}
