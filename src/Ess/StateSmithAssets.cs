using System.Runtime.InteropServices;

namespace Ess;

internal sealed record StateSmithAsset(string FileName, string Sha256)
{
    public bool IsZip => FileName.EndsWith(".zip", StringComparison.Ordinal);
}

internal sealed class StateSmithAssets
{
    public const string Version = "0.22.2";

    private readonly Dictionary<(HostOs, Architecture), StateSmithAsset> byPlatform = new()
    {
        [(HostOs.Linux, Architecture.X64)] = new("statesmith-linux-x64.tar.gz", "377f626092e9b1cb3a3a93664932dee5a467b8488fb9f0ebb3949fce8fc42a1f"),
        [(HostOs.Linux, Architecture.Arm64)] = new("statesmith-linux-arm64.tar.gz", "b1c101f0fe50f16b945fb0aa950d0445fba54fe571f3f220837ada91e6f32f13"),
        [(HostOs.Mac, Architecture.X64)] = new("statesmith-osx-x64.tar.gz", "085ea976a5ed1874fcccd713f7e73660974bfbaff8e9400ef4fda55c2f36e30c"),
        [(HostOs.Mac, Architecture.Arm64)] = new("statesmith-osx-arm64.tar.gz", "e2470e4affd6b54807ab4eb5be08705db64d16b7bf17ee06f1a8d45de8805573"),
        [(HostOs.Windows, Architecture.X64)] = new("statesmith-win-x64.zip", "235b05a2e3c948426d35455753fbc5543acd0b789e84f5e3eb1a011d498ddb0c"),
        [(HostOs.Windows, Architecture.Arm64)] = new("statesmith-win-arm64.zip", "834f98eef566f6cc75f3193a382ae73b69ee3000fea0eb9b92807a6e27e347fb"),
    };

    public StateSmithAsset? Find(HostPlatform platform)
    {
        return byPlatform.GetValueOrDefault((platform.Os, platform.Architecture));
    }

    public string DownloadUrl(StateSmithAsset asset)
    {
        return $"https://github.com/StateSmith/StateSmith/releases/download/v{Version}/{asset.FileName}";
    }
}
