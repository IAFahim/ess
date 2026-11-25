namespace Ess;

internal sealed class UserFolders
{
    public string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public string Bin => Path.Combine(Home, ".local", "bin");

    public string Data => Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } data
        ? data
        : Path.Combine(Home, ".local", "share");

    public string Config => Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } config
        ? config
        : Path.Combine(Home, ".config");

    public string Applications =>Path.Combine(Data, "applications");

    public string Mime => Path.Combine(Data, "mime");

    public string MimePackages => Path.Combine(Mime, "packages");

    public string ScalableIcons => Path.Combine(Data, "icons", "hicolor", "scalable", "apps");

    public bool HasScreen => Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") is { Length: > 0 }
        || Environment.GetEnvironmentVariable("DISPLAY") is { Length: > 0 };
}
