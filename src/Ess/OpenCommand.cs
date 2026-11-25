namespace Ess;

internal sealed class OpenCommand(
    OmaspecTool omaspec,
    DiagramFiles diagrams,
    DiagramLookup lookup,
    UserFolders folders,
    Subprocess subprocess,
    EssConsole console)
{
    private const string StartDetached = "exec setsid -f \"$0\" \"$@\" >/dev/null 2>&1";

    public async Task<int> Run(IReadOnlyList<string> arguments, string folder, CancellationToken cancel)
    {
        var program = await omaspec.EnsureNewest(cancel);
        if (program is null)
        {
            return 1;
        }

        await omaspec.MakeItOpenDiagrams(cancel);

        IReadOnlyList<string> targets;
        if (arguments.Count == 0)
        {
            targets = diagrams.In(folder);
            if (targets.Count == 0)
            {
                console.Line("Nothing to open: this folder has no diagrams.");
                return 0;
            }
        }
        else
        {
            var match = lookup.Find(arguments[0], folder);
            if (match.Path is null)
            {
                console.UsageError($"There is no \"{arguments[0]}\" here.");
                if (match.Suggestion is not null)
                {
                    console.UsageHint($"Did you mean:  ess o {match.Suggestion}");
                }

                return 1;
            }

            targets = [match.Path];
        }

        if (!folders.HasScreen)
        {
            console.Warn("There is no screen here, so I did not open omaspec.");
            return 0;
        }

        var started = await subprocess.Run("sh", ["-c", StartDetached, program, .. targets], folder, cancel, omaspec.LaunchEnvironment());
        if (!started.Succeeded)
        {
            console.Bad($"Could not start omaspec: {started.Output.Trim()}");
            return 1;
        }

        console.Good($"Opened {string.Join(", ", targets.Select(Path.GetFileName))} in omaspec.");
        return 0;
    }
}
