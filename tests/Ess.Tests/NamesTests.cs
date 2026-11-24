using Xunit;

namespace Ess.Tests;

public class NamesTests
{
    private readonly MachineNames machineNames = new();
    private readonly RepoNames repoNames = new();

    [Theory]
    [InlineData("door", "Door")]
    [InlineData("my cool door", "MyCoolDoor")]
    [InlineData("player-state_machine", "PlayerStateMachine")]
    [InlineData("3d game", "M3dGame")]
    [InlineData("!!!", "Machine")]
    [InlineData("", "Machine")]
    public void MachineNamesBecomeValidClassNames(string typed, string expected)
    {
        Assert.Equal(expected, machineNames.ToIdentifier(typed));
    }

    [Theory]
    [InlineData("/home/user/projects/my door", "MyDoorSm")]
    [InlineData("/home/user/projects/ess/", "EssSm")]
    public void DefaultMachineNameComesFromTheFolder(string folder, string expected)
    {
        Assert.Equal(expected, machineNames.DefaultForFolder(folder));
    }

    [Theory]
    [InlineData("/home/user/projects/my door", "my-door")]
    [InlineData("/home/user/projects/state.machine_1", "state.machine_1")]
    [InlineData("/home/user/projects/--odd--", "odd")]
    [InlineData("/home/user/projects/ess/", "ess")]
    [InlineData("/", "ess-project")]
    public void RepoNamesOnlyUseCharactersGitHubAllows(string folder, string expected)
    {
        Assert.Equal(expected, repoNames.FromFolder(folder));
    }
}
