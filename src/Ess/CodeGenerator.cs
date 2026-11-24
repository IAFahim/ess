namespace Ess;

internal sealed class CodeGenerator(
    StateSmithTool tool,
    Subprocess subprocess,
    GenerationOutputJudge judge,
    DiagramFiles diagrams,
    StructConverter structConverter,
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

        foreach (var fileName in written.Where(IsCSharp))
        {
            if (!await ConvertToStruct(Path.Combine(folder, fileName), fileName, cancel))
            {
                return false;
            }
        }

        foreach (var fileName in written)
        {
            console.Good(IsCSharp(fileName) ? $"made {fileName}  (a struct)" : $"made {fileName}");
        }

        return true;
    }

    private async Task<bool> ConvertToStruct(string path, string fileName, CancellationToken cancel)
    {
        var generatedText = await File.ReadAllTextAsync(path, cancel);
        var conversion = structConverter.Convert(generatedText);

        if (!conversion.Succeeded)
        {
            console.Bad($"Could not make {fileName} a struct: {conversion.Problem}");
            console.Line("    The file is left as StateSmith wrote it (a class). Nothing was saved to git.");
            return false;
        }

        if (conversion.Text != generatedText)
        {
            await File.WriteAllTextAsync(path, conversion.Text, cancel);
        }

        return true;
    }

    private static bool IsCSharp(string fileName) => fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
}
