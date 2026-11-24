using System.Reflection;

namespace Ess;

internal sealed class DemoFiles
{
    private readonly Assembly assembly = typeof(DemoFiles).Assembly;

    public async Task<IReadOnlyList<string>> WriteTo(string targetFolder, string resourceFolder, CancellationToken cancel)
    {
        var written = new List<string>();

        Directory.CreateDirectory(targetFolder);

        foreach (var (fileName, content) in await ReadAll(resourceFolder, cancel))
        {
            await File.WriteAllTextAsync(Path.Combine(targetFolder, fileName), content, cancel);
            written.Add(fileName);
        }

        return written;
    }

    public async Task<IReadOnlyList<(string FileName, string Content)>> ReadAll(string resourceFolder, CancellationToken cancel)
    {
        var prefix = $"demos/{resourceFolder}/";
        var files = new List<(string, string)>();

        foreach (var resourceName in assembly.GetManifestResourceNames().Where(name => name.StartsWith(prefix, StringComparison.Ordinal)).Order())
        {
            await using var source = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(source);
            files.Add((resourceName[prefix.Length..], await reader.ReadToEndAsync(cancel)));
        }

        return files;
    }
}
