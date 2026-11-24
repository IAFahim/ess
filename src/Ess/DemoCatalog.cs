namespace Ess;

internal sealed record DemoInfo(int Level, string FolderName, string Title, string ResourceFolder, bool RunsWithDotnet, string[] NextSteps);

internal sealed class DemoCatalog
{
    public IReadOnlyList<DemoInfo> All { get; } =
    [
        new(0, "demo-light", "a lamp: two states and a counter", "light", true,
            ["Edit LightSm.plantuml, save, and run  dotnet run  again.", "Or watch it:  cd demo-light  then  ess w"]),
        new(1, "demo-player", "a music player: nested states, a choice, history, your own code", "player", true,
            ["Read README.txt in the folder. It points at the part that surprises people.", "Edit PlayerSm.plantuml, then:  cd demo-player  then  ess w"]),
        new(2, "demo-unity-ecs", "Unity ECS: the struct as a component, driven by a Burst job", "ecs", false,
            ["Copy the folder into the Assets folder of a Unity 7 project.", "Then Window > General > Test Runner > EditMode > Run All. Expect 3 passed.", "README.txt in the folder lists the packages it needs."]),
    ];

    public DemoInfo? ForLevel(int level) => All.FirstOrDefault(demo => demo.Level == level);

    public string Listing => string.Join("  ", All.Select(demo => demo.Level == 0 ? "d" : $"d{demo.Level}"));
}
