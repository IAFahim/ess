namespace Ess;

internal sealed class DemoRequest
{
    public bool IsDemoWord(string word)
    {
        return word.Length is >= 1 and <= 4
            && word[0] == 'd'
            && word.Skip(1).All(char.IsAsciiDigit);
    }

    public int? LevelFrom(string typed, IReadOnlyList<string> extraArguments)
    {
        var word = typed.Trim().TrimStart('-').ToLowerInvariant();

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
