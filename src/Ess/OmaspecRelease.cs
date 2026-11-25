using System.Runtime.InteropServices;
using System.Text.Json;

namespace Ess;

internal sealed record OmaspecDownload(string Version, string FileName, string Url, string Sha256);

internal sealed class OmaspecRelease
{
    public const string LatestUrl = "https://api.github.com/repos/enhapu-afk/omaspec/releases/latest";

    private const string Sha256Prefix = "sha256:";
    private const int Sha256HexLength = 64;

    public string? ArchitectureName(Architecture architecture) => architecture switch
    {
        Architecture.X64 => "x86_64",
        Architecture.Arm64 => "aarch64",
        _ => null,
    };

    public OmaspecDownload? Pick(string releaseJson, string architectureName)
    {
        JsonDocument release;
        try
        {
            release = JsonDocument.Parse(releaseJson);
        }
        catch (JsonException)
        {
            return null;
        }

        using (release)
        {
            var root = release.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("tag_name", out var tag) || tag.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var version = tag.GetString()!.TrimStart('v');
            var fileName = $"omaspec-{version}-{architectureName}-linux.tar.gz";

            var asset = assets.EnumerateArray().FirstOrDefault(candidate => TextOf(candidate, "name") == fileName);
            var url = TextOf(asset, "browser_download_url");
            var digest = TextOf(asset, "digest");

            if (url is null || digest is null || !digest.StartsWith(Sha256Prefix, StringComparison.Ordinal))
            {
                return null;
            }

            var sha256 = digest[Sha256Prefix.Length..].ToLowerInvariant();
            return sha256.Length == Sha256HexLength ? new OmaspecDownload(version, fileName, url, sha256) : null;
        }
    }

    public string? VersionIn(string versionOutput)
    {
        var words = versionOutput.Split((char[])[' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
        return words.Length >= 2 && words[0] == "omaspec" ? words[1] : null;
    }

    public bool IsUpToDate(string installedVersion, string newestVersion)
    {
        return Version.TryParse(installedVersion, out var installed) && Version.TryParse(newestVersion, out var newest)
            ? installed >= newest
            : installedVersion == newestVersion;
    }

    private static string? TextOf(JsonElement element, string property)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }
}
