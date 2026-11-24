namespace Ess;

internal sealed class DiagramFiles
{
    private readonly string[] extensions = [".plantuml", ".puml", ".pu"];

    public bool IsDiagram(string path)
    {
        return extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> In(string folder)
    {
        return Directory.EnumerateFiles(folder)
            .Where(IsDiagram)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }
}
