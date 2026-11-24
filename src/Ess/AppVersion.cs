using System.Reflection;

namespace Ess;

internal sealed class AppVersion
{
    public string Current
    {
        get
        {
            var informational = typeof(AppVersion).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

            var buildMetadataStart = informational.IndexOf('+', StringComparison.Ordinal);
            return buildMetadataStart < 0 ? informational : informational[..buildMetadataStart];
        }
    }
}
