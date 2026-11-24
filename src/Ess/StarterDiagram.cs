namespace Ess;

internal sealed class StarterDiagram
{
    private const string Template = """
        @startuml {name}

        ' ess starter. Edit this file, save it, and ess makes the code again.
        ' To handle an event and stay in the same state, write it with no arrow:
        '   On : TICK / count += 1;

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
        UsePartialClass = true
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

    public string Render(string machineName) => Template.Replace("{name}", machineName, StringComparison.Ordinal) + "\n";
}
