namespace Ess;

internal sealed class ArgumentParser(CommandTable table, EditDistance editDistance)
{
    private const string EndOfOptions = "--";
    private const int LongestTypoWeForgive = 2;
    private const int ShortestWordWeGuessFor = 3;
    private const int ShortestWordWeCompareWith = 2;

    public Invocation Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return Invocation.NoArguments;
        }

        var optionsEnded = false;
        var wantsHelp = false;
        var wantsVersion = false;
        string? firstUnknownOption = null;
        string? commandWord = null;
        var positionals = new List<string>();

        foreach (var token in args)
        {
            if (!optionsEnded && token == EndOfOptions)
            {
                optionsEnded = true;
                continue;
            }

            if (!optionsEnded && IsOption(token))
            {
                var lowered = token.ToLowerInvariant();

                if (lowered is "-h" or "--help")
                {
                    wantsHelp = true;
                }
                else if (lowered == "--version")
                {
                    wantsVersion = true;
                }
                else
                {
                    firstUnknownOption ??= token;
                }

                continue;
            }

            if (commandWord is null)
            {
                commandWord = token;
            }
            else
            {
                positionals.Add(token);
            }
        }

        if (wantsVersion)
        {
            return Invocation.Version;
        }

        var command = commandWord is null ? null : table.Find(commandWord);

        if (commandWord is not null && command is null)
        {
            return UnknownCommand(commandWord);
        }

        if (wantsHelp)
        {
            return command is null ? Invocation.GlobalHelp : Invocation.HelpFor(command);
        }

        if (firstUnknownOption is not null)
        {
            var helpLine = command is null ? "ess --help" : $"ess {command.Name} --help";
            return Invocation.Failed($"Unknown option \"{firstUnknownOption}\". See:  {helpLine}");
        }

        return command is null ? Invocation.NoArguments : WithArguments(command, positionals);
    }

    private static Invocation WithArguments(CommandSpec command, List<string> positionals)
    {
        if (positionals.Count < command.MinArguments)
        {
            return Invocation.Failed($"\"ess {command.Name}\" needs {command.Arguments}. For example:  {command.Examples[^1]}");
        }

        if (positionals.Count > command.MaxArguments)
        {
            var allowed = command.MaxArguments == 0 ? "takes no arguments" : $"takes at most {command.MaxArguments}";
            return Invocation.Failed($"\"ess {command.Name}\" {allowed}, but got \"{positionals[command.MaxArguments]}\". See:  ess {command.Name} --help");
        }

        return Invocation.RunCommand(command, positionals);
    }

    private Invocation UnknownCommand(string commandWord)
    {
        var lowered = commandWord.ToLowerInvariant();
        var message = $"Unknown command \"{commandWord}\". See:  ess --help";

        var rememberedLine = RememberedLine(lowered);
        if (rememberedLine.Length > 0)
        {
            return Invocation.Failed(message, suggestedLine: rememberedLine);
        }

        if (lowered.Length < ShortestWordWeGuessFor)
        {
            return Invocation.Failed(message);
        }

        var closest = table.All
            .Select(spec => (Spec: spec, Distance: spec.Words
                .Where(word => word.Length >= ShortestWordWeCompareWith)
                .Min(word => editDistance.Between(lowered, word))))
            .OrderBy(candidate => candidate.Distance)
            .First();

        return closest.Distance <= LongestTypoWeForgive
            ? Invocation.Failed(message, closest.Spec, $"ess {closest.Spec.Name}")
            : Invocation.Failed(message);
    }

    private static string RememberedLine(string word)
    {
        if (word == "dsv")
        {
            return "ess d sv";
        }

        var isDemoWithNumber = word.Length > 1 && word[0] == 'd' && word.Skip(1).All(char.IsAsciiDigit);
        return isDemoWithNumber ? "ess d" : "";
    }

    private static bool IsOption(string token)
    {
        var isNegativeNumber = token.Length > 1 && token.Skip(1).All(char.IsAsciiDigit);
        return token.Length > 1 && token[0] == '-' && !isNegativeNumber;
    }
}
