using System.Text.RegularExpressions;

namespace Ess;

internal sealed partial class ScreenCards
{
    private const string DrmFolder = "/sys/class/drm";
    private const string Connected = "connected";

    private readonly string[] builtInPanels = ["-eDP-", "-LVDS-", "-DSI-"];

    public bool PreferStrongCard()
    {
        try
        {
            if (!Directory.Exists(DrmFolder))
            {
                return false;
            }

            var names = Directory.EnumerateFileSystemEntries(DrmFolder).Select(Path.GetFileName).OfType<string>().ToList();
            var connected = names
                .Where(name => name.Contains('-') && ReadStatus(name) == Connected)
                .ToList();

            return PreferStrongCard(names, connected);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public bool PreferStrongCard(IReadOnlyList<string> drmNames, IReadOnlyList<string> connectedConnectors)
    {
        var cardCount = drmNames.Count(name => CardName().IsMatch(name));
        var hasBuiltInPanel = connectedConnectors.Any(connector => builtInPanels.Any(panel => connector.Contains(panel, StringComparison.Ordinal)));

        return cardCount >= 2 && connectedConnectors.Count > 0 && !hasBuiltInPanel;
    }

    private static string ReadStatus(string connector)
    {
        var statusPath = Path.Combine(DrmFolder, connector, "status");
        return File.Exists(statusPath) ? File.ReadAllText(statusPath).Trim() : "";
    }

    [GeneratedRegex(@"^card\d+$")]
    private static partial Regex CardName();
}
