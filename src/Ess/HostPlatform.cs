using System.Runtime.InteropServices;

namespace Ess;

public enum HostOs
{
    Linux,
    Mac,
    Windows,
    Other,
}

internal readonly record struct HostPlatform(HostOs Os, Architecture Architecture)
{
    public static HostPlatform Detect()
    {
        var os = OperatingSystem.IsLinux() ? HostOs.Linux
            : OperatingSystem.IsMacOS() ? HostOs.Mac
            : OperatingSystem.IsWindows() ? HostOs.Windows
            : HostOs.Other;

        return new HostPlatform(os, RuntimeInformation.OSArchitecture);
    }

    public override string ToString() => $"{Os} {Architecture}";
}
