namespace Ess;

internal sealed class RunCommand(CodeGenerator generator, DotnetFolder dotnetFolder, DotnetRunner runner)
{
    public async Task<int> Run(string folder, CancellationToken cancel)
    {
        if (!await generator.Make(folder, cancel))
        {
            return 1;
        }

        if (!dotnetFolder.HasProject(folder))
        {
            return 0;
        }

        return await runner.Run(Path.Combine(folder, DotnetFolder.FolderName), DotnetFolder.HowToRun, cancel) ? 0 : 1;
    }
}
