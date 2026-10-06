namespace ValheimModManager.Tests;

using ValheimModManager.Core.Models;
using Xunit;

public class ModVersionComparerTests
{
    [Theory]
    [InlineData("1.0.1", "1.0.0", true)]
    [InlineData("2.0.0", "1.9.9", true)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("0.9.5", "1.0.0", false)]
    [InlineData("v1.5.0", "1.4.9", true)]
    [InlineData("5.4.2351", "5.4.2300", true)]
    [InlineData("1.2.0-rc.2", "1.2.0-rc.1", true)]
    [InlineData("1.2.0", "1.2.0-rc.2", true)]
    public void IsNewer_EvaluatesCorrectly(string remote, string installed, bool expected)
    {
        var result = ModVersionComparer.IsNewer(remote, installed);
        Assert.Equal(expected, result);
    }
}
