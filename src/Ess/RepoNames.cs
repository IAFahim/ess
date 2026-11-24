using System.Text;

namespace Ess;

internal sealed class RepoNames
{
    private const string NameWhenNothingUsable = "ess-project";

    public string FromFolder(string folder)
    {
        var folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder));
        var repoName = new StringBuilder();

        foreach (var character in folderName)
        {
            var allowed = char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_';
            repoName.Append(allowed ? character : '-');
        }

        var trimmed = repoName.ToString().Trim('-', '.');
        return trimmed.Length == 0 ? NameWhenNothingUsable : trimmed;
    }
}
