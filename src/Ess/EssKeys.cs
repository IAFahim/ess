namespace Ess;

internal sealed class EssKeys
{
    private const char QuitKey = 'q';

    public char Read()
    {
        if (Console.IsInputRedirected)
        {
            return ReadFromPipe();
        }

        var pressed = Console.ReadKey(intercept: true);
        return pressed.Key is ConsoleKey.Escape or ConsoleKey.Enter
            ? QuitKey
            : char.ToLowerInvariant(pressed.KeyChar);
    }

    private static char ReadFromPipe()
    {
        while (true)
        {
            var next = Console.In.Read();
            if (next < 0)
            {
                return QuitKey;
            }

            if (!char.IsWhiteSpace((char)next))
            {
                return char.ToLowerInvariant((char)next);
            }
        }
    }
}
