using Xunit;

namespace Ess.Tests;

public class SdkListTests
{
    private readonly SdkList sdkList = new();

    [Theory]
    [InlineData("10.0.401 [/home/user/.dotnet/sdk]\n", 10, true)]
    [InlineData("8.0.100 [/usr/share/dotnet/sdk]\n10.0.100 [/usr/share/dotnet/sdk]\n", 10, true)]
    [InlineData("11.0.100 [/opt/dotnet/sdk]\n", 10, true)]
    [InlineData("9.0.300 [/usr/share/dotnet/sdk]\n", 10, false)]
    [InlineData("", 10, false)]
    [InlineData("No .NET SDKs were found.\n", 10, false)]
    [InlineData("10.0.401 [/a]\r\n", 10, true)]
    public void FindsAnSdkOfTheRequiredMajorOrNewer(string output, int major, bool expected)
    {
        Assert.Equal(expected, sdkList.HasAtLeast(output, major));
    }
}
