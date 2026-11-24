namespace Ess;

public enum EssCommand
{
    New,
    Run,
    Watch,
    Git,
    Demo,
    Update,
}

internal sealed record CommandSpec(
    EssCommand Command,
    string Name,
    string[] Aliases,
    string Arguments,
    int MinArguments,
    int MaxArguments,
    string Summary,
    string[] Examples)
{
    public IEnumerable<string> Words => Aliases.Prepend(Name);

    public string Key => Aliases.First(alias => alias.Length == 1);

    public string Brief
    {
        get
        {
            var firstSentenceEnd = Summary.IndexOf('.', StringComparison.Ordinal);
            return firstSentenceEnd < 0 ? Summary : Summary[..firstSentenceEnd];
        }
    }
}

internal sealed class CommandTable
{
    public IReadOnlyList<CommandSpec> All { get; } =
    [
        new(EssCommand.New, "new", ["n", "in"], "[what]", 0, 1,
            "Make a starter diagram in this folder. \"ess n sv\" gets the sv engine instead. The word sv is reserved, it cannot be a machine name.",
            ["ess n", "ess n door", "ess n sv"]),
        new(EssCommand.Run, "run", ["r"], "", 0, 0,
            "Make the code once.",
            ["ess r"]),
        new(EssCommand.Watch, "watch", ["w"], "", 0, 0,
            "Make the code every time you save. While watching, g saves to GitHub now and q stops.",
            ["ess w"]),
        new(EssCommand.Git, "git", ["g", "save", "s"], "", 0, 0,
            "Save to git and push to GitHub. It makes a private repo if there is none.",
            ["ess g"]),
        new(EssCommand.Demo, "demo", ["d"], "[which]", 0, 1,
            "List the demos. Give a number or a name to make one in a new folder.",
            ["ess d", "ess d 1", "ess d light", "ess d sv"]),
        new(EssCommand.Update, "update", ["u"], "<what>", 1, 1,
            "Get the latest of something. For now only sv.",
            ["ess u sv"]),
    ];

    public IReadOnlyList<string> GlobalExamples { get; } = ["ess d", "ess n sv", "ess u sv", "ess w"];

    public CommandSpec? Find(string word)
    {
        var wanted = word.Trim().ToLowerInvariant();
        return All.FirstOrDefault(spec => spec.Words.Contains(wanted));
    }
}
