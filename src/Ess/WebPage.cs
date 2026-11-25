using System.Net;

namespace Ess;

internal sealed class WebPage
{
    public const string SimulatorEnding = ".sim.html";
    public const string IndexFileName = "index.html";
    public const string NoJekyllFileName = ".nojekyll";

    private const string Template = """
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>{title}</title>
        <style>
        :root { color-scheme: light dark; }
        body { font-family: system-ui, sans-serif; font-size: 1.25rem; line-height: 1.6; max-width: 40rem; margin: 3rem auto; padding: 0 1rem; }
        a { display: block; padding: 0.75rem 1rem; margin: 0.5rem 0; border: 2px solid currentColor; border-radius: 0.5rem; text-decoration: none; }
        </style>
        </head>
        <body>
        <h1>{title}</h1>
        <p>Pick a machine. Click its events and watch it change.</p>
        {links}
        </body>
        </html>

        """;

    public bool IsSimulator(string path) => path.EndsWith(SimulatorEnding, StringComparison.OrdinalIgnoreCase);

    public string MachineName(string path) => path[..^SimulatorEnding.Length];

    private static string Address(string path) => string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    public string Index(string title, IEnumerable<string> simulators)
    {
        var links = simulators
            .Order(StringComparer.Ordinal)
            .Select(path => $"<a href=\"{WebUtility.HtmlEncode(Address(path))}\">{WebUtility.HtmlEncode(MachineName(path))}</a>");

        return Template
            .Replace("{title}", WebUtility.HtmlEncode(title), StringComparison.Ordinal)
            .Replace("{links}", string.Join('\n', links), StringComparison.Ordinal);
    }
}
