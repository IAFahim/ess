using Xunit;

namespace Ess.Tests;

public class StructConverterTests
{
    private readonly StructConverter converter = new();
    private readonly GeneratedFixtures fixtures = new();

    [Theory]
    [InlineData("StarterSm")]
    [InlineData("PlainSm")]
    [InlineData("NonPartialSm")]
    [InlineData("EcsSm")]
    [InlineData("GlobalSm")]
    [InlineData("PlayerSm")]
    public void EveryGeneratedClassBecomesAPartialStruct(string machineName)
    {
        var conversion = converter.Convert(fixtures.Text(machineName));

        Assert.True(conversion.Succeeded, conversion.Problem);
        Assert.Contains($"public partial struct {machineName}", conversion.Text);
        Assert.DoesNotMatch(@"public (partial )?class", conversion.Text);
        Assert.DoesNotContain("new Vars()", conversion.Text);
        Assert.DoesNotContain($"public {machineName}()", conversion.Text);
        Assert.StartsWith(StructConverter.ConvertedNote, conversion.Text);
    }

    [Fact]
    public void TheBaseListSurvives()
    {
        var conversion = converter.Convert(fixtures.Text("EcsSm"));

        Assert.Contains("public partial struct EcsSm : Unity.Entities.IComponentData", conversion.Text);
    }

    [Fact]
    public void ConvertingTwiceChangesNothing()
    {
        var once = converter.Convert(fixtures.Text("PlayerSm"));
        var twice = converter.Convert(once.Text);

        Assert.True(twice.Succeeded);
        Assert.Equal(once.Text, twice.Text);
    }

    [Fact]
    public void WindowsLineEndingsConvertToo()
    {
        var windowsText = fixtures.Text("StarterSm").Replace("\n", "\r\n", StringComparison.Ordinal);

        var conversion = converter.Convert(windowsText);

        Assert.True(conversion.Succeeded, conversion.Problem);
        Assert.Contains("public partial struct StarterSm", conversion.Text);
        Assert.DoesNotContain("new Vars()", conversion.Text);
        Assert.StartsWith(StructConverter.ConvertedNote + "\r\n", conversion.Text);
    }

    [Fact]
    public void AFileScopedNamespaceIsRefusedBecauseUnityCannotCompileIt()
    {
        var conversion = converter.Convert(fixtures.Text("ScopedSm"));

        Assert.False(conversion.Succeeded);
        Assert.Contains("braces", conversion.Problem);
    }

    [Fact]
    public void AConstructorWithCodeIsRefused()
    {
        var withCode = fixtures.Text("StarterSm").Replace(
            "public StarterSm()\n        {\n        }",
            "public StarterSm()\n        {\n            vars.count = 5;\n        }",
            StringComparison.Ordinal);

        var conversion = converter.Convert(withCode);

        Assert.False(conversion.Succeeded);
        Assert.Contains("constructor", conversion.Problem);
    }

    [Fact]
    public void TwoClassesAreRefused()
    {
        var text = fixtures.Text("StarterSm");

        var conversion = converter.Convert(text + "\n    public class Extra { }\n");

        Assert.False(conversion.Succeeded);
        Assert.Contains("exactly one", conversion.Problem);
    }

    [Fact]
    public void CodeThatIsNotAStateMachineIsRefused()
    {
        Assert.False(converter.Convert("namespace X { }").Succeeded);
    }
}
