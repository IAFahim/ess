using System.Runtime.InteropServices;
using Xunit;

namespace Ess.Tests;

public class OmaspecTests
{
    private const string Sha = "0327aceca0c02e765bceb5a23e1abc4c4c5505a85fe8488b503a0c7563cc66be";

    private const string ReleaseJson = $$"""
        {
          "tag_name": "v1.2.1",
          "assets": [
            { "name": "omaspec-1.2.1-aarch64-linux.tar.gz", "browser_download_url": "https://example.test/arm.tar.gz", "digest": "sha256:{{Sha}}" },
            { "name": "omaspec-1.2.1-x86_64-linux.tar.gz.sha256", "browser_download_url": "https://example.test/x.sha256", "digest": "sha256:{{Sha}}" },
            { "name": "omaspec-1.2.1-x86_64-linux.tar.gz", "browser_download_url": "https://example.test/x.tar.gz", "digest": "sha256:{{Sha}}" }
          ]
        }
        """;

    private readonly OmaspecRelease release = new();

    [Fact]
    public void TheNewestReleaseGivesTheArchiveForThisComputerAndItsFingerprint()
    {
        var download = release.Pick(ReleaseJson, "x86_64");

        Assert.NotNull(download);
        Assert.Equal("1.2.1", download.Version);
        Assert.Equal("omaspec-1.2.1-x86_64-linux.tar.gz", download.FileName);
        Assert.Equal("https://example.test/x.tar.gz", download.Url);
        Assert.Equal(Sha, download.Sha256);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"message":"API rate limit exceeded"}""")]
    [InlineData("""{"tag_name":"v1.2.1","assets":[{"name":"omaspec-1.2.1-x86_64-linux.tar.gz","browser_download_url":"https://example.test/x.tar.gz"}]}""")]
    [InlineData("""{"tag_name":"v1.2.1","assets":[{"name":"omaspec-1.2.1-x86_64-linux.tar.gz","browser_download_url":"https://example.test/x.tar.gz","digest":"sha256:abc"}]}""")]
    public void AReleaseWithoutAFingerprintedArchiveGivesNothing(string json)
    {
        Assert.Null(release.Pick(json, "x86_64"));
    }

    [Fact]
    public void OnlyX64AndArm64HaveAnArchive()
    {
        Assert.Equal("x86_64", release.ArchitectureName(Architecture.X64));
        Assert.Equal("aarch64", release.ArchitectureName(Architecture.Arm64));
        Assert.Null(release.ArchitectureName(Architecture.Arm));
    }

    [Theory]
    [InlineData("omaspec 1.2.1\n", "1.2.1")]
    [InlineData("omaspec 1.10.0", "1.10.0")]
    [InlineData("something else 1.0", null)]
    [InlineData("", null)]
    public void TheVersionComesFromTheProgramsOwnAnswer(string output, string? expected)
    {
        Assert.Equal(expected, release.VersionIn(output));
    }

    [Theory]
    [InlineData("1.2.1", "1.2.1", true)]
    [InlineData("1.10.0", "1.9.0", true)]
    [InlineData("1.2.0", "1.2.1", false)]
    [InlineData("1.9.0", "1.10.0", false)]
    [InlineData("1.3.0-rc1", "1.3.0-rc1", true)]
    [InlineData("1.3.0-rc1", "1.3.0", false)]
    public void UpToDateComparesVersionsAsNumbers(string installed, string newest, bool expected)
    {
        Assert.Equal(expected, release.IsUpToDate(installed, newest));
    }

    private static readonly Dictionary<string, string> NoEnvironment = [];

    private static readonly Dictionary<string, string> StrongCard = new() { ["WGPU_POWER_PREF"] = "high" };

    [Fact]
    public void TheDesktopEntryRunsTheInstalledProgramAndListsEveryDiagramType()
    {
        const string entry = "[Desktop Entry]\nName=Omaspec\nExec=omaspec %F\nMimeType=text/x-plantuml;text/vnd.mermaid;\n";

        var written = new DesktopEntry().PointedAt(entry, "/home/me/.local/bin/omaspec", NoEnvironment, ["text/vnd.plantuml", "text/x-plantuml"]);

        Assert.Equal(
            "[Desktop Entry]\nName=Omaspec\nExec=/home/me/.local/bin/omaspec %F\nMimeType=text/x-plantuml;text/vnd.mermaid;text/vnd.plantuml;\n",
            written);
    }

    [Fact]
    public void TheDesktopEntryCanAskForTheStrongCard()
    {
        var written = new DesktopEntry().PointedAt("Exec=omaspec %F\n", "/bin/omaspec", StrongCard, []);

        Assert.Equal("Exec=env WGPU_POWER_PREF=high /bin/omaspec %F\n", written);
    }

    [Fact]
    public void PointingAnEntryAgainChangesNothingAndCanDropTheStrongCard()
    {
        var desktopEntry = new DesktopEntry();
        var strong = desktopEntry.PointedAt("Exec=omaspec %F\n", "/bin/omaspec", StrongCard, ["text/vnd.plantuml"]);

        Assert.Equal(strong, desktopEntry.PointedAt(strong, "/bin/omaspec", StrongCard, ["text/vnd.plantuml"]));
        Assert.Equal("Exec=/bin/omaspec %F\nMimeType=text/vnd.plantuml;\n", desktopEntry.PointedAt(strong, "/bin/omaspec", NoEnvironment, ["text/vnd.plantuml"]));
    }

    [Fact]
    public void AProgramPathWithASpaceIsQuoted()
    {
        var written = new DesktopEntry().PointedAt("Exec=omaspec %F", "/home/my name/.local/bin/omaspec", NoEnvironment, []);

        Assert.Equal("Exec=\"/home/my name/.local/bin/omaspec\" %F\n", written);
    }

    [Theory]
    [InlineData(new[] { "card0", "card0-DP-3", "card0-HDMI-A-1", "card1", "card1-DP-1", "renderD128", "version" }, new[] { "card0-DP-3", "card0-HDMI-A-1" }, true)]
    [InlineData(new[] { "card0", "card0-eDP-1", "card1", "card1-HDMI-A-1" }, new[] { "card0-eDP-1" }, false)]
    [InlineData(new[] { "card0", "card0-eDP-1", "card1", "card1-HDMI-A-1" }, new[] { "card0-eDP-1", "card1-HDMI-A-1" }, false)]
    [InlineData(new[] { "card0", "card0-HDMI-A-1" }, new[] { "card0-HDMI-A-1" }, false)]
    [InlineData(new[] { "card0", "card1" }, new string[0], false)]
    public void OnlyADesktopWithTwoCardsAsksForTheStrongOne(string[] drmNames, string[] connected, bool expected)
    {
        Assert.Equal(expected, new ScreenCards().PreferStrongCard(drmNames, connected));
    }
}
