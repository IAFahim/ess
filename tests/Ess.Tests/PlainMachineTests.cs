using Xunit;

namespace Ess.Tests;

public class PlainMachineTests
{
    private readonly StructConverter converter = new();
    private readonly GeneratedFixtures fixtures = new();
    private readonly CSharp9Compilation compilation = new();
    private readonly UnityComponentFile componentFile = new();

    [Theory]
    [InlineData("StarterSm", "Machines.StarterSm")]
    [InlineData("PlainSm", "Machines.PlainSm")]
    [InlineData("GlobalSm", "GlobalSm")]
    public void APlainMachineCompilesWithNoUnityAtAll(string fixtureName, string typeName)
    {
        var converted = converter.Convert(fixtures.Text(fixtureName)).Text;

        var compiled = compilation.CompileWithoutUnity(converted, typeName, fixtureName + "NoUnity");

        Assert.Empty(compiled.Errors);
        Assert.Empty(compiled.Warnings);
        Assert.True(compiled.MachineIsUnmanaged);
        Assert.False(compiled.MachineIsComponent);
    }

    [Theory]
    [InlineData("StarterSm", "Machines.StarterSm")]
    [InlineData("PlainSm", "Machines.PlainSm")]
    [InlineData("GlobalSm", "GlobalSm")]
    public void TheUnityFileMakesThePlainMachineAComponent(string fixtureName, string typeName)
    {
        var converted = converter.Convert(fixtures.Text(fixtureName)).Text;
        var machine = componentFile.Read(converted);
        Assert.NotNull(machine);

        var compiled = compilation.CompileWithComponentFile(converted, componentFile.Text(machine.Value), typeName, fixtureName + "WithUnity");

        Assert.Empty(compiled.Errors);
        Assert.Empty(compiled.Warnings);
        Assert.True(compiled.MachineIsUnmanaged);
        Assert.True(compiled.MachineIsComponent);
    }

    [Theory]
    [InlineData("StarterSm", "Machines", "StarterSm")]
    [InlineData("GlobalSm", null, "GlobalSm")]
    public void ReadsTheMachineNameAndNamespace(string fixtureName, string? expectedNamespace, string expectedName)
    {
        var converted = converter.Convert(fixtures.Text(fixtureName)).Text;

        Assert.Equal(new MachineType(expectedNamespace, expectedName), componentFile.Read(converted));
    }

    [Fact]
    public void AClassIsNotReadAsAMachine()
    {
        Assert.Null(componentFile.Read(fixtures.Text("StarterSm")));
    }

    [Fact]
    public void TheComponentFileLivesInTheUnityFolder()
    {
        Assert.Equal("Unity/DoorSm.Component.cs", componentFile.RelativePath("DoorSm"));
    }

    [Fact]
    public void TheDotnetProgramRunsTheStarterMachine()
    {
        var converted = converter.Convert(fixtures.Text("StarterSm")).Text;
        var program = new DotnetFolder(new MachineNames()).Program("StarterSm");

        var compiled = compilation.CompileProgram(converted, program, "Machines.StarterSm", "StarterProgram");

        Assert.Empty(compiled.Errors);
        Assert.Empty(compiled.Warnings);
        Assert.DoesNotContain("{name}", program);
    }

    [Fact]
    public void TheDotnetProjectIsCSharp9AndLeavesTheUnityFolderOut()
    {
        var project = new DotnetFolder(new MachineNames()).Project();

        Assert.Contains("<LangVersion>9.0</LangVersion>", project);
        Assert.Contains("<Compile Include=\"../*.cs\" />", project);
        Assert.Contains("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", project);
        Assert.DoesNotContain("Unity", project);
    }

    [Fact]
    public void TheDotnetFolderIsHiddenFromUnity()
    {
        Assert.EndsWith("~", DotnetFolder.FolderName);
    }
}
