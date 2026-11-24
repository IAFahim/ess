namespace Ess;

internal sealed class DemoRequest
{
    private const string SvDemoWord = "dsv";

    public bool IsSvDemo(string typed, IReadOnlyList<string> extraArguments)
    {
        var word = typed.Trim().TrimStart('-').ToLowerInvariant();

        return word == SvDemoWord
            || (word is "demo" or "demos" && extraArguments.Count > 0 && extraArguments[0].Trim().ToLowerInvariant() == "sv");
    }

    public bool IsDemoWord(string word)
    {
        return word == SvDemoWord
            || word.Length is >= 1 and <= 4
            && word[0] == 'd'
            && word.Skip(1).All(char.IsAsciiDigit);
    }

    public int? LevelFrom(string typed, IReadOnlyList<string> extraArguments)
    {
        var word = typed.Trim().TrimStart('-').ToLowerInvariant();

        if (word == SvDemoWord)
        {
            return null;
        }

        if (IsDemoWord(word) && word.Length > 1)
        {
            return int.Parse(word.AsSpan(1));
        }

        if (extraArguments.Count > 0)
        {
            return int.TryParse(extraArguments[0], out var typedLevel) ? typedLevel : null;
        }

        return 0;
    }
}
