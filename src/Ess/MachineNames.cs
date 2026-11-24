using System.Text;

namespace Ess;

internal sealed class MachineNames
{
    private const string NameWhenNothingUsable = "Machine";

    public string ToIdentifier(string typed)
    {
        var identifier = new StringBuilder();
        var startOfWord = true;

        foreach (var character in typed)
        {
            if (!char.IsAsciiLetterOrDigit(character))
            {
                startOfWord = true;
                continue;
            }

            identifier.Append(startOfWord ? char.ToUpperInvariant(character) : character);
            startOfWord = false;
        }

        if (identifier.Length == 0)
        {
            return NameWhenNothingUsable;
        }

        return char.IsAsciiDigit(identifier[0]) ? "M" + identifier : identifier.ToString();
    }

    public string DefaultForFolder(string folder)
    {
        var folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder));
        return ToIdentifier(folderName) + "Sm";
    }
}
