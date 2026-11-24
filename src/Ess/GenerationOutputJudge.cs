using System.Text.RegularExpressions;

namespace Ess;

internal sealed partial class GenerationOutputJudge
{
    private readonly string[] failureMarkers = ["Finished with failure", "Failed while trying"];
    private readonly string[] problemStartMarkers = ["Failed while trying", "Exception "];
    private readonly string[] problemEndMarkers = ["Related error info", "StateSmith Runner - Finished"];

    public bool Succeeded(ProcessResult result)
    {
        return result.ExitCode == 0
            && !failureMarkers.Any(marker => result.Output.Contains(marker, StringComparison.Ordinal));
    }

    public IReadOnlyList<string> FilesWritten(string output)
    {
        return WrittenFileLine().Matches(output)
            .Select(match => match.Groups["file"].Value)
            .Distinct()
            .ToList();
    }

    public string Problem(string output)
    {
        var lines = output.Split('\n', StringSplitOptions.TrimEntries);
        var firstProblemLine = Array.FindIndex(lines, line => problemStartMarkers.Any(marker => line.Contains(marker, StringComparison.Ordinal)));

        if (firstProblemLine < 0)
        {
            return output.Trim();
        }

        var problemLines = lines
            .Skip(firstProblemLine)
            .TakeWhile(line => !problemEndMarkers.Any(marker => line.StartsWith(marker, StringComparison.Ordinal)))
            .Where(line => line.Length > 0);

        return string.Join(Environment.NewLine, problemLines);
    }

    [GeneratedRegex("Writing to file `(?<file>[^`]+)`")]
    private static partial Regex WrittenFileLine();
}
