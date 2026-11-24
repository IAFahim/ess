using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Ess;

internal sealed class StateSmithTool(EssHome home, StateSmithAssets assets, HttpClient http, EssConsole console)
{
    private readonly string[] executableNames = ["ss.cli", "ss.cli.exe"];

    private const UnixFileMode RunnableByEveryone =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
        | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
        | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

    public async Task<string?> EnsureInstalled(CancellationToken cancel)
    {
        var platform = HostPlatform.Detect();
        var asset = assets.Find(platform);
        if (asset is null)
        {
            console.Bad($"StateSmith has no download for this computer ({platform}).");
            return null;
        }

        var toolDirectory = home.ToolDirectory(StateSmithAssets.Version);
        var alreadyInstalled = FindExecutable(toolDirectory);
        if (alreadyInstalled is not null)
        {
            return alreadyInstalled;
        }

        console.Step($"First time only: getting StateSmith {StateSmithAssets.Version} (about 55 MB).");

        try
        {
            return await Install(asset, toolDirectory, cancel);
        }
        catch (HttpRequestException problem)
        {
            console.Bad($"Could not download StateSmith. Are you online? ({problem.Message})");
            return null;
        }
        catch (IOException problem)
        {
            console.Bad($"Could not unpack StateSmith: {problem.Message}");
            return null;
        }
    }

    private async Task<string?> Install(StateSmithAsset asset, string toolDirectory, CancellationToken cancel)
    {
        Directory.CreateDirectory(home.DownloadDirectory);
        var archivePath = Path.Combine(home.DownloadDirectory, asset.FileName);

        await Download(assets.DownloadUrl(asset), archivePath, cancel);

        var actualHash = await HashOf(archivePath, cancel);
        if (!string.Equals(actualHash, asset.Sha256, StringComparison.Ordinal))
        {
            File.Delete(archivePath);
            console.Bad("The StateSmith download did not match its fingerprint, so I threw it away.");
            return null;
        }

        var stagingDirectory = toolDirectory + ".partial";
        if (Directory.Exists(stagingDirectory))
        {
            Directory.Delete(stagingDirectory, recursive: true);
        }

        Directory.CreateDirectory(stagingDirectory);
        await Unpack(asset, archivePath, stagingDirectory, cancel);
        MakeRunnable(stagingDirectory);

        Directory.Move(stagingDirectory, toolDirectory);
        File.Delete(archivePath);

        console.Good("StateSmith is ready.");
        return FindExecutable(toolDirectory);
    }

    private async Task Download(string url, string destinationPath, CancellationToken cancel)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancel);
        response.EnsureSuccessStatusCode();

        await using var destination = File.Create(destinationPath);
        await response.Content.CopyToAsync(destination, cancel);
    }

    private static async Task<string> HashOf(string path, CancellationToken cancel)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancel));
    }

    private static async Task Unpack(StateSmithAsset asset, string archivePath, string destinationDirectory, CancellationToken cancel)
    {
        if (asset.IsZip)
        {
            await ZipFile.ExtractToDirectoryAsync(archivePath, destinationDirectory, cancel);
            return;
        }

        await using var compressed = File.OpenRead(archivePath);
        await using var tarStream = new GZipStream(compressed, CompressionMode.Decompress);
        await TarFile.ExtractToDirectoryAsync(tarStream, destinationDirectory, overwriteFiles: true, cancel);
    }

    private void MakeRunnable(string directory)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        foreach (var executable in ExecutablesIn(directory))
        {
            File.SetUnixFileMode(executable, RunnableByEveryone);
        }
    }

    private string? FindExecutable(string directory)
    {
        return Directory.Exists(directory) ? ExecutablesIn(directory).FirstOrDefault() : null;
    }

    private IEnumerable<string> ExecutablesIn(string directory)
    {
        return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(path => executableNames.Contains(Path.GetFileName(path)));
    }
}
