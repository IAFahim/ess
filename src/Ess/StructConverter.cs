using System.Text.RegularExpressions;

namespace Ess;

internal readonly record struct StructConversion(bool Succeeded, string Text, string Problem);

internal sealed partial class StructConverter
{
    public const string ConvertedNote = "// Converted from a class to a struct by ess so it is unmanaged data (Unity ECS, Burst). Pass it by ref.";

    private const string VarsInitializer = "public Vars vars = new Vars();";
    private const string VarsField = "public Vars vars;";

    public StructConversion Convert(string generatedText)
    {
        if (generatedText.Contains(ConvertedNote, StringComparison.Ordinal))
        {
            return new StructConversion(true, generatedText, "");
        }

        if (FileScopedNamespace().IsMatch(generatedText))
        {
            return Failed("NameSpace in the diagram ends with \";\". Unity needs the braces form. Remove the \";\".");
        }

        var classDeclarations = ClassDeclaration().Matches(generatedText);
        if (classDeclarations.Count != 1)
        {
            return Failed($"Expected exactly one state machine class, found {classDeclarations.Count}.");
        }

        if (!generatedText.Contains(VarsInitializer, StringComparison.Ordinal))
        {
            return Failed($"Did not find \"{VarsInitializer}\" in the generated code.");
        }

        var machineName = classDeclarations[0].Groups["name"].Value;
        var asStruct = ClassDeclaration().Replace(generatedText, "${indent}public partial struct ${name}${bases}", 1);
        var withoutInitializer = asStruct.Replace(VarsInitializer, VarsField, StringComparison.Ordinal);

        var lineEnding = generatedText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var withoutConstructor = RemoveEmptyConstructor(withoutInitializer, machineName);
        if (withoutConstructor is null)
        {
            return Failed($"The constructor of {machineName} has code in it. A struct cannot have one, move that code into Start.");
        }

        return new StructConversion(true, ConvertedNote + lineEnding + withoutConstructor, "");
    }

    private static string? RemoveEmptyConstructor(string text, string machineName)
    {
        var lines = text.Split('\n').ToList();
        var constructorLine = lines.FindIndex(line => line.Trim() == $"public {machineName}()");

        if (constructorLine < 0)
        {
            return text;
        }

        var bodyIsEmpty = constructorLine + 2 < lines.Count
            && lines[constructorLine + 1].Trim() == "{"
            && lines[constructorLine + 2].Trim() == "}";

        if (!bodyIsEmpty)
        {
            return null;
        }

        var first = constructorLine;
        if (first > 0 && lines[first - 1].TrimStart().StartsWith("//", StringComparison.Ordinal))
        {
            first--;
        }

        var count = constructorLine + 3 - first;
        var blankLineAfter = constructorLine + 3 < lines.Count && lines[constructorLine + 3].Trim().Length == 0;
        lines.RemoveRange(first, blankLineAfter ? count + 1 : count);

        return string.Join('\n', lines);
    }

    private static StructConversion Failed(string problem) => new(false, "", problem);

    [GeneratedRegex(@"^namespace\s+[\w.]+\s*;", RegexOptions.Multiline)]
    private static partial Regex FileScopedNamespace();

    [GeneratedRegex(@"^(?<indent>[ \t]*)public (?:partial )?class (?<name>\w+)(?<bases>[^\r\n{]*)", RegexOptions.Multiline)]
    private static partial Regex ClassDeclaration();
}
