namespace Ess;

internal sealed class UpdateCommand(SvCommand sv, EssConsole console)
{
    private const string SvWord = "sv";

    public async Task<int> Run(IReadOnlyList<string> arguments, string folder, CancellationToken cancel)
    {
        if (arguments[0].Trim().Equals(SvWord, StringComparison.OrdinalIgnoreCase))
        {
            return await sv.Update(folder, cancel);
        }

        console.UsageError($"I cannot update \"{arguments[0]}\". For now only:  ess u sv");
        return 1;
    }
}
