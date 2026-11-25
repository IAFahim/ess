using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Ess;

internal sealed class OmaspecTool(
    EssHome home,
    UserFolders folders,
    OmaspecRelease release,
    DesktopEntry desktopEntry,
    HttpClient http,
    Subprocess subprocess,
    EssConsole console)
{
    private const string ProgramName = "omaspec";
    private const string DesktopFileName = "omaspec.desktop";
    private const string PackagedMimeFileName = "omaspec-mime.xml";
    private const string MimeFileName = "omaspec.xml";
    private const string IconFileName = "omaspec.svg";

    private const UnixFileMode RunnableByEveryone =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
        | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
        | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

    private readonly string[] diagramTypes = ["text/vnd.plantuml", "text/x-plantuml", "text/vnd.mermaid"];

    private string ProgramPath => Path.Combine(folders.Bin, ProgramName);

    private string DesktopPath => Path.Combine(folders.Applications, DesktopFileName);

    private string MimePath => Path.Combine(folders.MimePackages, MimeFileName);

    public async Task<string?> EnsureNewest(CancellationToken cancel)
    {
        var platform = HostPlatform.Detect();
        var architectureName = release.ArchitectureName(platform.Architecture);
        if (!OperatingSystem.IsLinux() || architectureName is null)
        {
            console.Bad($"omaspec only runs on Linux (this is {platform}). To see a machine, open the .sim.html file next to its diagram in a browser.");
            return null;
        }

        var installedVersion = await InstalledVersion(cancel);

        OmaspecDownload? newest;
        try
        {
            newest = await Newest(architectureName, cancel);
        }
        catch (HttpRequestException problem)
        {
            return KeepInstalled(installedVersion, $"Could not ask GitHub for the newest omaspec. Are you online? ({problem.Message})");
        }

        if (newest is null)
        {
            return KeepInstalled(installedVersion, "The newest omaspec release has no download for this computer.");
        }

        var filesInPlace = File.Exists(DesktopPath) && File.Exists(MimePath);
        if (installedVersion is not null && filesInPlace && release.IsUpToDate(installedVersion, newest.Version))
        {
            console.Good($"omaspec {installedVersion} is the newest.");
            return ProgramPath;
        }

        console.Step(installedVersion is null
            ? $"Getting omaspec {newest.Version}, the diagram viewer."
            : $"Updating omaspec {installedVersion} to {newest.Version}.");

        try
        {
            return await Install(newest, cancel) ? ProgramPath : null;
        }
        catch (HttpRequestException problem)
        {
            console.Bad($"Could not download omaspec. Are you online? ({problem.Message})");
            return null;
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            console.Bad($"Could not put omaspec in place: {problem.Message}");
            return null;
        }
    }

    public async Task MakeItOpenDiagrams(CancellationToken cancel)
    {
        if (!File.Exists(DesktopPath))
        {
            console.Warn("Run  ess o  again when you are online, so omaspec opens diagram files.");
            return;
        }

        var entryText = await File.ReadAllTextAsync(DesktopPath, cancel);
        var pointedEntry = desktopEntry.PointedAt(entryText, ProgramPath, diagramTypes);
        if (pointedEntry != entryText)
        {
            await File.WriteAllTextAsync(DesktopPath, pointedEntry, cancel);
        }

        var mimeDatabase = await subprocess.Run("update-mime-database", [folders.Mime], folders.Home, cancel);
        if (!mimeDatabase.Succeeded)
        {
            console.Warn(mimeDatabase.ProgramMissing
                ? "update-mime-database is missing (package shared-mime-info), so files do not know they are diagrams."
                : $"update-mime-database failed: {mimeDatabase.Output.Trim()}");
        }

        await subprocess.Run("update-desktop-database", [folders.Applications], folders.Home, cancel);

        Directory.CreateDirectory(folders.Config);
        var setDefault = await subprocess.Run("xdg-mime", ["default", DesktopFileName, .. diagramTypes], folders.Home, cancel);
        if (!setDefault.Succeeded)
        {
            console.Warn(setDefault.ProgramMissing
                ? "xdg-mime is missing (package xdg-utils), so I could not make omaspec open diagram files."
                : $"xdg-mime could not make omaspec open diagram files: {setDefault.Output.Trim()}");
            return;
        }

        var check = await subprocess.Run("xdg-mime", ["query", "default", diagramTypes[0]], folders.Home, cancel);
        if (check.Output.Contains(DesktopFileName, StringComparison.Ordinal))
        {
            console.Good("Opening a .plantuml or .puml file now opens omaspec.");
        }
        else
        {
            console.Warn($"Could not make omaspec open .plantuml files. xdg-mime says: {check.Output.Trim()}");
        }
    }

    private string? KeepInstalled(string? installedVersion, string problem)
    {
        if (installedVersion is null)
        {
            console.Bad(problem);
            return null;
        }

        console.Warn($"{problem} I kept omaspec {installedVersion}.");
        return ProgramPath;
    }

    private async Task<string?> InstalledVersion(CancellationToken cancel)
    {
        if (!File.Exists(ProgramPath))
        {
            return null;
        }

        var result = await subprocess.Run(ProgramPath, ["--version"], folders.Home, cancel);
        return result.Succeeded ? release.VersionIn(result.Output) : null;
    }

    private async Task<OmaspecDownload?> Newest(string architectureName, CancellationToken cancel)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, OmaspecRelease.LatestUrl);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = await http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();

        return release.Pick(await response.Content.ReadAsStringAsync(cancel), architectureName);
    }

    [SupportedOSPlatform("linux")]
    private async Task<bool> Install(OmaspecDownload newest, CancellationToken cancel)
    {
        Directory.CreateDirectory(home.DownloadDirectory);
        var archivePath = Path.Combine(home.DownloadDirectory, newest.FileName);
        var unpackDirectory = archivePath + ".partial";

        try
        {
            await Download(newest.Url, archivePath, cancel);

            if (await HashOf(archivePath, cancel) != newest.Sha256)
            {
                console.Bad("The omaspec download did not match its fingerprint, so I threw it away.");
                return false;
            }

            await Unpack(archivePath, unpackDirectory, cancel);

            var program = FindFile(unpackDirectory, ProgramName);
            var entry = FindFile(unpackDirectory, DesktopFileName);
            var mime = FindFile(unpackDirectory, PackagedMimeFileName);
            if (program is null || entry is null || mime is null)
            {
                console.Bad("The omaspec download is missing files, so I left everything as it was.");
                return false;
            }

            Directory.CreateDirectory(folders.Bin);
            var staged = ProgramPath + ".new";
            File.Copy(program, staged, overwrite: true);
            File.SetUnixFileMode(staged, RunnableByEveryone);
            File.Move(staged, ProgramPath, overwrite: true);

            Directory.CreateDirectory(folders.Applications);
            var entryText = await File.ReadAllTextAsync(entry, cancel);
            await File.WriteAllTextAsync(DesktopPath, desktopEntry.PointedAt(entryText, ProgramPath, diagramTypes), cancel);

            Directory.CreateDirectory(folders.MimePackages);
            File.Copy(mime, MimePath, overwrite: true);

            var icon = FindFile(unpackDirectory, IconFileName);
            if (icon is not null)
            {
                Directory.CreateDirectory(folders.ScalableIcons);
                File.Copy(icon, Path.Combine(folders.ScalableIcons, IconFileName), overwrite: true);
            }

            console.Good($"omaspec {newest.Version} is ready: {ProgramPath}");
            return true;
        }
        finally
        {
            File.Delete(archivePath);
            if (Directory.Exists(unpackDirectory))
            {
                Directory.Delete(unpackDirectory, recursive: true);
            }
        }
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

    private static async Task Unpack(string archivePath, string destinationDirectory, CancellationToken cancel)
    {
        if (Directory.Exists(destinationDirectory))
        {
            Directory.Delete(destinationDirectory, recursive: true);
        }

        Directory.CreateDirectory(destinationDirectory);

        await using var compressed = File.OpenRead(archivePath);
        await using var tarStream = new GZipStream(compressed, CompressionMode.Decompress);
        await TarFile.ExtractToDirectoryAsync(tarStream, destinationDirectory, overwriteFiles: true, cancel);
    }

    private static string? FindFile(string directory, string fileName)
    {
        return Directory.EnumerateFiles(directory, fileName, SearchOption.AllDirectories).FirstOrDefault();
    }
}
