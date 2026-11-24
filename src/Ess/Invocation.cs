namespace Ess;

internal enum InvocationKind
{
    NoArguments,
    GlobalHelp,
    CommandHelp,
    Version,
    Command,
    Error,
}

internal sealed record Invocation(
    InvocationKind Kind,
    CommandSpec? Command,
    IReadOnlyList<string> Arguments,
    string Error,
    CommandSpec? SuggestedCommand,
    string SuggestedLine)
{
    public static Invocation NoArguments { get; } = new(InvocationKind.NoArguments, null, [], "", null, "");

    public static Invocation GlobalHelp { get; } = new(InvocationKind.GlobalHelp, null, [], "", null, "");

    public static Invocation Version { get; } = new(InvocationKind.Version, null, [], "", null, "");

    public static Invocation HelpFor(CommandSpec command) => new(InvocationKind.CommandHelp, command, [], "", null, "");

    public static Invocation RunCommand(CommandSpec command, IReadOnlyList<string> arguments) => new(InvocationKind.Command, command, arguments, "", null, "");

    public static Invocation Failed(string error, CommandSpec? suggestedCommand = null, string suggestedLine = "") =>
        new(InvocationKind.Error, null, [], error, suggestedCommand, suggestedLine);
}
