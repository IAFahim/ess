namespace Ess;

internal enum ResolutionKind
{
    ShowMenu,
    ShowVersion,
    RunCommand,
    DidYouMean,
    Unknown,
}

internal readonly record struct CommandResolution(ResolutionKind Kind, CommandInfo? Command);

internal sealed class CommandResolver(CommandCatalog catalog, EditDistance editDistance, DemoRequest demoRequest)
{
    private const int LongestTypoWeForgive = 2;
    private const int ShortestWordWeGuessFor = 3;
    private const int ShortestPrefixWeAccept = 2;

    private readonly string[] menuWords = ["", "?", "h", "help", "menu", "m"];
    private readonly string[] versionWords = ["v", "version"];

    public CommandResolution Resolve(string typed)
    {
        var wanted = typed.Trim().TrimStart('-').ToLowerInvariant();

        if (menuWords.Contains(wanted))
        {
            return new CommandResolution(ResolutionKind.ShowMenu, null);
        }

        if (versionWords.Contains(wanted))
        {
            return new CommandResolution(ResolutionKind.ShowVersion, null);
        }

        if (demoRequest.IsDemoWord(wanted))
        {
            return new CommandResolution(ResolutionKind.RunCommand, catalog.All.First(command => command.Command == EssCommand.Demo));
        }

        var exactMatch = catalog.All.FirstOrDefault(command => WordsOf(command).Contains(wanted));
        if (exactMatch is not null)
        {
            return new CommandResolution(ResolutionKind.RunCommand, exactMatch);
        }

        var prefixMatches = catalog.All
            .Where(command => wanted.Length >= ShortestPrefixWeAccept && command.Name.StartsWith(wanted, StringComparison.Ordinal))
            .ToList();
        if (prefixMatches.Count == 1)
        {
            return new CommandResolution(ResolutionKind.RunCommand, prefixMatches[0]);
        }

        if (wanted.Length < ShortestWordWeGuessFor)
        {
            return new CommandResolution(ResolutionKind.Unknown, null);
        }

        var closest = catalog.All
            .Select(command => (Command: command, Distance: DistanceToClosestWord(wanted, command)))
            .OrderBy(candidate => candidate.Distance)
            .First();

        return closest.Distance <= LongestTypoWeForgive
            ? new CommandResolution(ResolutionKind.DidYouMean, closest.Command)
            : new CommandResolution(ResolutionKind.Unknown, null);
    }

    private int DistanceToClosestWord(string wanted, CommandInfo command)
    {
        return WordsOf(command)
            .Where(word => word.Length >= ShortestPrefixWeAccept)
            .Min(word => editDistance.Between(wanted, word));
    }

    private static IEnumerable<string> WordsOf(CommandInfo command)
    {
        return command.OtherWords.Append(command.Key).Append(command.Name);
    }
}
