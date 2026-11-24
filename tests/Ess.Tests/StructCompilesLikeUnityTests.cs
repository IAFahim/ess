using Xunit;

namespace Ess.Tests;

public class StructCompilesLikeUnityTests
{
    private readonly StructConverter converter = new();
    private readonly GeneratedFixtures fixtures = new();
    private readonly CSharp9Compilation compilation = new();

    [Theory]
    [InlineData("StarterSm", "Machines.StarterSm")]
    [InlineData("PlainSm", "Machines.PlainSm")]
    [InlineData("NonPartialSm", "Machines.NonPartialSm")]
    [InlineData("EcsSm", "Machines.EcsSm")]
    [InlineData("GlobalSm", "GlobalSm")]
    [InlineData("PlayerSm", "Machines.PlayerSm")]
    public void ConvertedMachinesCompileAtCSharp9WithNoErrorsOrWarningsAndAreUnmanaged(string fixtureName, string typeName)
    {
        var converted = converter.Convert(fixtures.Text(fixtureName)).Text;

        var compiled = compilation.Compile(converted, typeName, fixtureName + "Struct");

        Assert.Empty(compiled.Errors);
        Assert.Empty(compiled.Warnings);
        Assert.True(compiled.MachineIsUnmanaged);
    }

    [Fact]
    public void TheGeneratedClassIsNotUnmanagedWhichIsWhyWeConvert()
    {
        var compiled = compilation.Compile(fixtures.Text("PlayerSm"), "Machines.PlayerSm", "PlayerClass");

        Assert.Empty(compiled.Errors);
        Assert.False(compiled.MachineIsUnmanaged);
    }

    [Fact]
    public void JustSwappingClassForStructBreaksAtCSharp9()
    {
        var naive = fixtures.Text("StarterSm").Replace("partial class", "partial struct", StringComparison.Ordinal);

        var compiled = compilation.Compile(naive, "Machines.StarterSm", "NaiveStruct");

        Assert.NotEmpty(compiled.Errors);
        Assert.Contains(compiled.Errors, error => error.Contains("CS8773", StringComparison.Ordinal));
    }
}
