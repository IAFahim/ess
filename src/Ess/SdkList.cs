namespace Ess;

internal sealed class SdkList
{
    public bool HasAtLeast(string listSdksOutput, int major)
    {
        return listSdksOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('.')[0])
            .Any(first => int.TryParse(first, out var sdkMajor) && sdkMajor >= major);
    }
}
