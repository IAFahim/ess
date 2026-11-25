namespace Ess;

internal sealed class DesktopEntry
{
    private const string ExecKey = "Exec=";
    private const string MimeTypeKey = "MimeType=";

    public string PointedAt(string entry, string programPath, IReadOnlyList<string> types)
    {
        var quoted = programPath.Contains(' ') ? $"\"{programPath}\"" : programPath;
        var lines = entry.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').ToList();

        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].StartsWith(ExecKey, StringComparison.Ordinal))
            {
                var command = lines[index][ExecKey.Length..];
                var firstSpace = command.IndexOf(' ');
                var arguments = firstSpace < 0 ? "" : command[firstSpace..];
                lines[index] = ExecKey + quoted + arguments;
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
}
