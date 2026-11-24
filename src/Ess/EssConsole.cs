namespace Ess;

internal sealed class EssConsole(TextWriter output, bool useColor)
{
    public void Line(string text = "") => output.WriteLine(text);

    public void Step(string text) => Tagged("36", "..  ", text);

    public void Good(string text) => Tagged("32", "ok  ", text);

    public void Warn(string text) => Tagged("33", "!   ", text);

    public void Bad(string text) => Tagged("31", "x   ", text);

    private void Tagged(string colorCode, string tag, string text)
    {
        output.WriteLine(useColor ? $"\e[{colorCode}m{tag}\e[0m{text}" : tag + text);
    }
}
