namespace Ess;

internal sealed class DemoSelector(DemoCatalog catalog)
{
    public DemoInfo? Find(string which)
    {
        var cleaned = which.Trim().TrimStart('-').ToLowerInvariant();

        return int.TryParse(cleaned, out var number)
            ? catalog.All.FirstOrDefault(demo => demo.Number == number)
            : catalog.All.FirstOrDefault(demo => demo.Name == cleaned);
    }
}
