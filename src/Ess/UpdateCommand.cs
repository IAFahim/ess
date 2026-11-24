namespace Ess;

internal sealed class UpdateCommand(SvCommand sv, EssConsole console)
{
    public async Task<int> Run(IReadOnlyList<string> extraArguments, string folder, CancellationToken cancel)
    {
        var what = extraArguments.Count > 0 ? extraArguments[0].Trim().ToLowerInvariant() : "";

        if (what == "sv")
        {
            return await sv.Update(folder, cancel);
        }

        console.Warn("Update what?  ess u sv");
        return 1;
    }
}
