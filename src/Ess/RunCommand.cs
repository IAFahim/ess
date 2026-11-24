namespace Ess;

internal sealed class RunCommand(CodeGenerator generator)
{
    public async Task<int> Run(string folder, CancellationToken cancel)
    {
        return await generator.Make(folder, cancel) ? 0 : 1;
    }
}
