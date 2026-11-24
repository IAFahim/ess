namespace Ess;

internal sealed record DemoInfo(
    int Number,
    string Name,
    string FolderName,
    string Title,
    string ResourceFolder,
    bool RunsWithDotnet,
    bool IsSvEngine,
    string[] NextSteps);

internal sealed class DemoCatalog
{
    public IReadOnlyList<DemoInfo> All { get; } =
    [
        new(1, "light", "demo-light", "a lamp: two states and a counter", "light", true, false,
            ["Edit LightSm.plantuml, save, and run  dotnet run  again.", "Or watch it:  cd demo-light  then  ess w"]),
        new(2, "player", "demo-player", "a music player: nested states, a choice, history, your own code", "player", true, false,
            ["Read README.txt in the folder. It points at the part that surprises people.", "Edit PlayerSm.plantuml, then:  cd demo-player  then  ess w"]),
        new(3, "ecs", "demo-unity-ecs", "Unity ECS: the struct as a component, driven by a Burst job", "ecs", false, false,
            ["Copy the folder into the Assets folder of a Unity 7 project.", "Then Window > General > Test Runner > EditMode > Run All. Expect 3 passed.", "README.txt in the folder lists the packages it needs."]),
        new(4, "sv", "demo-sv", "sv and StateSmith together: sv keeps the numbers, a StateSmith struct follows its events", "", false, true, []),
    ];

    public IReadOnlyList<string> ListLines()
    {
        var nameWidth = All.Max(demo => demo.Name.Length);

        var lines = new List<string> { "Demos. Make one with:  ess d <number or name>", "" };
        lines.AddRange(All.Select(demo => $"  {demo.Number}  {demo.Name.PadRight(nameWidth)}  {demo.Title}"));
        return lines;
    }
}
