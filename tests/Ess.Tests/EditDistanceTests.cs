using Xunit;

namespace Ess.Tests;

public class EditDistanceTests
{
    private readonly EditDistance editDistance = new();

    [Theory]
    [InlineData("watch", "watch", 0)]
    [InlineData("wach", "watch", 1)]
    [InlineData("wathc", "watch", 1)]
    [InlineData("gti", "git", 1)]
    [InlineData("", "git", 3)]
    [InlineData("run", "ru", 1)]
    [InlineData("kitten", "sitting", 3)]
    public void CountsInsertsDeletesSubstitutionsAndSwappedLetters(string first, string second, int expected)
    {
        Assert.Equal(expected, editDistance.Between(first, second));
        Assert.Equal(expected, editDistance.Between(second, first));
    }
}
