using System.Runtime.InteropServices;
using Xunit;

namespace Ess.Tests;

public class StateSmithAssetsTests
{
    private readonly StateSmithAssets assets = new();

    [Theory]
    [InlineData(HostOs.Linux, Architecture.X64, "statesmith-linux-x64.tar.gz")]
    [InlineData(HostOs.Linux, Architecture.Arm64, "statesmith-linux-arm64.tar.gz")]
    [InlineData(HostOs.Mac, Architecture.X64, "statesmith-osx-x64.tar.gz")]
    [InlineData(HostOs.Mac, Architecture.Arm64, "statesmith-osx-arm64.tar.gz")]
    [InlineData(HostOs.Windows, Architecture.X64, "statesmith-win-x64.zip")]
    [InlineData(HostOs.Windows, Architecture.Arm64, "statesmith-win-arm64.zip")]
    public void EveryPlatformWeSupportHasItsDownload(HostOs os, Architecture architecture, string expectedFile)
    {
        var asset = assets.Find(new HostPlatform(os, architecture));

        Assert.NotNull(asset);
        Assert.Equal(expectedFile, asset.FileName);
        Assert.Equal(64, asset.Sha256.Length);
        Assert.Equal(expectedFile.EndsWith(".zip", StringComparison.Ordinal), asset.IsZip);
    }

    [Fact]
    public void UnsupportedPlatformsHaveNoDownload()
    {
        Assert.Null(assets.Find(new HostPlatform(HostOs.Other, Architecture.X64)));
        Assert.Null(assets.Find(new HostPlatform(HostOs.Linux, Architecture.Arm)));
    }

    [Fact]
    public void DownloadUrlPointsAtThePinnedRelease()
    {
        var asset = assets.Find(new HostPlatform(HostOs.Linux, Architecture.X64))!;

        Assert.Equal(
            "https://github.com/StateSmith/StateSmith/releases/download/v0.22.2/statesmith-linux-x64.tar.gz",
            assets.DownloadUrl(asset));
    }
}
