using System.Reflection;

namespace Ess;

internal sealed class DemoFiles
{
    private readonly Assembly assembly = typeof(DemoFiles).Assembly;

    public async Task<IReadOnlyList<string>> WriteTo(string targetFolder, string resourceFolder, CancellationToken cancel)
    {
        var prefix = $"demos/{resourceFolder}/";
        var written = new List<string>();

        Directory.CreateDirectory(targetFolder);

        foreach (var resourceName in assembly.GetManifestResourceNames().Where(name => name.StartsWith(prefix, StringComparison.Ordinal)).Order())
        {
            var fileName = resourceName[prefix.Length..];
            await using var source = assembly.GetManifestResourceStream(resourceName)!;
            await using var destination = File.Create(Path.Combine(targetFolder, fileName));
            await source.CopyToAsync(destination, cancel);
            written.Add(fileName);
        }

        return written;
    }
}
