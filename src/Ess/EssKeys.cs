namespace Ess;

internal sealed class EssKeys
{
    private const char QuitKey = 'q';

    private readonly TimeSpan pollInterval = TimeSpan.FromMilliseconds(50);

    public async Task<char> ReadAsync(CancellationToken cancel)
    {
        if (Console.IsInputRedirected)
        {
            return ReadFromPipe();
        }

        try
        {
            while (true)
            {
                var pressed = ReadIfPressed();
                if (pressed is not null)
                {
                    return pressed.Value;
                }

                await Task.Delay(pollInterval, cancel);
            }
        }
        catch (OperationCanceledException)
        {
            return QuitKey;
        }
    }

    public char? ReadIfPressed()
    {
        if (Console.IsInputRedirected || !Console.KeyAvailable)
        {
            return null;
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
