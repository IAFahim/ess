namespace Ess;

internal sealed class CodeGenerator(
    StateSmithTool tool,
    Subprocess subprocess,
    GenerationOutputJudge judge,
    DiagramFiles diagrams,
    EssConsole console)
{
    private readonly string[] stateSmithArguments = ["run", "--here", "--no-csx", "--no-ask", "--rebuild"];

    public async Task<bool> Make(string folder, CancellationToken cancel)
    {
        if (diagrams.In(folder).Count == 0)
        {
            console.Warn("There is no diagram in this folder. Run:  ess n");
            return false;
        }

        var executable = await tool.EnsureInstalled(cancel);
        if (executable is null)
        {
            return false;
        }

        var result = await subprocess.Run(executable, stateSmithArguments, folder, cancel);

        if (!judge.Succeeded(result))
        {
            console.Bad("The diagram has a problem. Nothing was made:");
            console.Line();
            console.Line(judge.Problem(result.Output));
            return false;
        }

        var written = judge.FilesWritten(result.Output);
        foreach (var fileName in written)
        {
            console.Good($"made {fileName}");
        }

        return true;
    }
}
