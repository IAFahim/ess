namespace Ess;

internal sealed class DesktopEntry
{
    private const string ExecKey = "Exec=";
    private const string MimeTypeKey = "MimeType=";

    public string PointedAt(string entry, string programPath, IReadOnlyDictionary<string, string> environment, IReadOnlyList<string> types)
    {
        var lines = entry.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').ToList();

        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].StartsWith(ExecKey, StringComparison.Ordinal))
            {
                lines[index] = ExecKey + Command(programPath, environment, lines[index][ExecKey.Length..]);
            }
            else if (lines[index].StartsWith(MimeTypeKey, StringComparison.Ordinal))
            {
                var listed = lines[index][MimeTypeKey.Length..].Split(';', StringSplitOptions.RemoveEmptyEntries);
                lines[index] = MimeTypeKey + string.Concat(listed.Union(types, StringComparer.Ordinal).Select(type => type + ";"));
            }
        }

        if (types.Count > 0 && !lines.Any(line => line.StartsWith(MimeTypeKey, StringComparison.Ordinal)))
        {
            lines.Add(MimeTypeKey + string.Concat(types.Select(type => type + ";")));
        }

        return string.Join('\n', lines) + "\n";
    }

    private static string Command(string programPath, IReadOnlyDictionary<string, string> environment, string oldCommand)
    {
        var words = new List<string>();

        if (environment.Count > 0)
        {
            words.Add("env");
            words.AddRange(environment.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"));
        }

        words.Add(programPath.Contains(' ') ? $"\"{programPath}\"" : programPath);
        words.AddRange(oldCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(word => word.StartsWith('%')));

        return string.Join(' ', words);
    }
}
