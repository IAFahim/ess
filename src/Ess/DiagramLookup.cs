namespace Ess;

internal sealed record DiagramMatch(string? Path, string? Suggestion);

internal sealed class DiagramLookup(DiagramFiles diagrams, EditDistance editDistance)
{
    private const int LongestTypoWeForgive = 2;
    private const int ShortestStartWeOpen = 3;

    public DiagramMatch Find(string wanted, string folder)
    {
        var path = Path.GetFullPath(wanted, folder);
        if (File.Exists(path) || Directory.Exists(path))
        {
            return new DiagramMatch(path, null);
        }

        var searchFolder = Path.GetDirectoryName(path);
        if (searchFolder is null || !Directory.Exists(searchFolder))
        {
            return new DiagramMatch(null, null);
        }

        var name = Path.GetFileName(path).ToLowerInvariant();
        var candidates = diagrams.In(searchFolder);

        var meant = OnlyOne(candidates.Where(candidate => NamesOf(candidate).Contains(name)))
            ?? (name.Length >= ShortestStartWeOpen ? OnlyOne(candidates.Where(candidate => StemOf(candidate).StartsWith(name, StringComparison.Ordinal))) : null);
        if (meant is not null)
        {
            return new DiagramMatch(meant, null);
        }

        var closest = candidates
            .Select(candidate => (Path: candidate, Distance: DistanceTo(name, candidate)))
            .OrderBy(candidate => candidate.Distance)
            .FirstOrDefault();

        return closest.Path is not null && closest.Distance <= LongestTypoWeForgive
            ? new DiagramMatch(null, System.IO.Path.GetRelativePath(folder, closest.Path))
            : new DiagramMatch(null, null);
    }

    private int DistanceTo(string name, string candidate)
    {
        var stem = StemOf(candidate);
        var names = name.Length >= ShortestStartWeOpen
            ? NamesOf(candidate).Append(stem[..Math.Min(stem.Length, name.Length)])
            : NamesOf(candidate);
        return names.Min(candidateName => editDistance.Between(name, candidateName));
    }

    private static string? OnlyOne(IEnumerable<string> paths)
    {
        var found = paths.Take(2).ToList();
        return found.Count == 1 ? found[0] : null;
    }

    private static string StemOf(string diagramPath) => Path.GetFileNameWithoutExtension(diagramPath).ToLowerInvariant();

    private static string[] NamesOf(string diagramPath) => [Path.GetFileName(diagramPath).ToLowerInvariant(), StemOf(diagramPath)];
}
