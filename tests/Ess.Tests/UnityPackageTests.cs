using System.Text.Json;
using Xunit;

namespace Ess.Tests;

public class UnityPackageTests : IDisposable
{
    private readonly UnityPackage package = new(new MachineNames());
    private readonly string workFolder = Path.Combine(Path.GetTempPath(), "ess-unity-" + Guid.NewGuid().ToString("N"));

    public UnityPackageTests() => Directory.CreateDirectory(workFolder);

    public void Dispose() => Directory.Delete(workFolder, recursive: true);

    [Theory]
    [InlineData("/home/user/my-game", "com.ess.my-game")]
    [InlineData("/home/user/My Game", "com.ess.my-game")]
    [InlineData("/home/user/hero_machines", "com.ess.hero-machines")]
    [InlineData("/home/user/Door", "com.ess.door")]
    [InlineData("/home/user/--", "com.ess.machines")]
    public void PackageNamesAreWhatUnityAccepts(string folder, string expected)
    {
        Assert.Equal(expected, package.PackageName(folder));
    }

    [Fact]
    public void ManifestIsValidJsonWithTheFieldsUnityNeeds()
    {
        using var manifest = JsonDocument.Parse(package.Manifest("/home/user/my-game"));
        var root = manifest.RootElement;

        Assert.Equal("com.ess.my-game", root.GetProperty("name").GetString());
        Assert.Equal("0.1.0", root.GetProperty("version").GetString());
        Assert.Equal("MyGame machines", root.GetProperty("displayName").GetString());
        Assert.Equal("7000.0", root.GetProperty("unity").GetString());
        Assert.True(root.GetProperty("dependencies").TryGetProperty("com.unity.entities", out _));
    }

    [Fact]
    public void AssemblyDefinitionReferencesWhatTheMadeStructNeeds()
    {
        using var assembly = JsonDocument.Parse(package.AssemblyDefinition("/home/user/my-game"));
        var root = assembly.RootElement;
        var references = root.GetProperty("references").EnumerateArray().Select(reference => reference.GetString()).ToList();

        Assert.Equal("Ess.MyGame", root.GetProperty("name").GetString());
        Assert.Equal("Ess.MyGame.asmdef", package.AssemblyFileName("/home/user/my-game"));
        Assert.Contains("Unity.Entities", references);
        Assert.Contains("Unity.Burst", references);
        Assert.True(root.GetProperty("autoReferenced").GetBoolean());
    }

    [Fact]
    public void ReadmeTellsHowToAddThePackage()
    {
        var readme = package.Readme("/home/user/my-game");

        Assert.Contains("Install package from disk", readme);
        Assert.Contains("Ess.MyGame", readme);
        Assert.Contains("Dotnet~", readme);
        Assert.Contains("Unity/", readme);
        Assert.DoesNotContain("{", readme);
    }

    [Fact]
    public void FindsTheUnityProjectAroundAFolder()
    {
        var settings = Path.Combine(workFolder, "ProjectSettings");
        var inside = Path.Combine(workFolder, "Assets", "Machines");
        Directory.CreateDirectory(settings);
        Directory.CreateDirectory(inside);
        File.WriteAllText(Path.Combine(settings, "ProjectVersion.txt"), "m_EditorVersion: 7000.0.0a20\n");

        Assert.Equal(workFolder, package.UnityProjectAround(inside));
    }

    [Fact]
    public void AFolderOutsideUnityHasNoUnityProject()
    {
        Assert.Null(package.UnityProjectAround(workFolder));
    }

    [Fact]
    public void AFolderIsAPackageOnceItHasAManifestAndAnAssemblyDefinition()
    {
        Assert.False(package.IsPackage(workFolder));

        File.WriteAllText(Path.Combine(workFolder, UnityPackage.ManifestFileName), package.Manifest(workFolder));
        Assert.False(package.IsPackage(workFolder));

        File.WriteAllText(Path.Combine(workFolder, package.AssemblyFileName(workFolder)), package.AssemblyDefinition(workFolder));
        Assert.True(package.IsPackage(workFolder));
    }
}
