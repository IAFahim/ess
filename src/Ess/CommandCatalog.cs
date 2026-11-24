namespace Ess;

public enum EssCommand
{
    New,
    Run,
    Watch,
    Git,
    Demo,
}

internal sealed record CommandInfo(EssCommand Command, string Key, string Name, string Meaning, string[] OtherWords);

internal sealed class CommandCatalog
{
    public IReadOnlyList<CommandInfo> All { get; } =
    [
        new(EssCommand.New, "n", "new", "make a starter diagram here", ["init", "in", "i", "create"]),
        new(EssCommand.Run, "r", "run", "make the code one time", ["go", "make", "build"]),
        new(EssCommand.Watch, "w", "watch", "make the code every time you save", ["live"]),
        new(EssCommand.Git, "g", "git", "save and push to GitHub", ["save", "s", "push", "sync"]),
        new(EssCommand.Demo, "d", "demo", "make a demo to try  (d, d1, d2)", ["demos"]),
    ];
}
