namespace Ess.Tests;

internal sealed class GeneratedFixtures
{
    public string Text(string machineName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "generated", machineName + ".cs"));
    }
}
