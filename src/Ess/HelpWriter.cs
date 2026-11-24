namespace Ess;

internal sealed class HelpWriter(CommandTable table)
{
    private const int NamesWidth = 18;
    private const int ArgumentsWidth = 8;

    public IReadOnlyList<string> Global()
    {
        var lines = new List<string>
        {
            "ess - easy StateSmith",
            "",
            "Usage: ess <command> [arguments] [options]",
            "",
            "Commands:",
        };

        lines.AddRange(table.All.Select(spec =>
            $"  {string.Join(", ", spec.Words),-NamesWidth} {spec.Arguments,-ArgumentsWidth} {spec.Brief}."));

        lines.Add("");
        lines.Add("Options:");
        lines.Add("  -h, --help    Show help. It works after any command too:  ess d --help");
        lines.Add("  --version     Show the version.");
        lines.Add("");
        lines.Add("Examples:");
        lines.AddRange(table.GlobalExamples.Select(example => $"  {example}"));

        return lines;
    }

    public IReadOnlyList<string> For(CommandSpec spec)
    {
        var usage = $"Usage: ess {spec.Name} {spec.Arguments}".TrimEnd() + " [options]";

        var lines = new List<string> { usage, "", spec.Summary, "" };

        if (spec.Aliases.Length > 0)
        {
            lines.Add($"Short forms: {string.Join(", ", spec.Aliases)}");
            lines.Add("");
        }

        lines.Add("Examples:");
        lines.AddRange(spec.Examples.Select(example => $"  {example}"));
        lines.Add("");
        lines.Add("Options:");
        lines.Add("  -h, --help    Show this help.");

        return lines;
    }

    public IReadOnlyList<string> Menu()
    {
        return table.All.Where(spec => spec.MinArguments == 0).Select(spec => $"  {spec.Key}   {spec.Name,-7} {spec.Brief}").ToList();
    }
}
