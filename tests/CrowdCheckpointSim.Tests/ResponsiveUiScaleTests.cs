using System.Numerics;
using CrowdCheckpointSim.UI;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class ResponsiveUiScaleTests
{
    [Fact]
    public void ReferenceResolutionUsesUnitScale()
    {
        float scale = ResponsiveUiScale.Calculate(new Vector2(1500f, 900f));

        Assert.Equal(1f, scale, 3);
    }

    [Fact]
    public void LargerMatchingDisplayScalesProportionally()
    {
        float scale = ResponsiveUiScale.Calculate(new Vector2(3000f, 1800f));

        Assert.Equal(2f, scale, 3);
    }

    [Fact]
    public void FourKDisplayRetainsReferenceProportions()
    {
        float scale = ResponsiveUiScale.Calculate(new Vector2(3840f, 2160f));

        Assert.Equal(2.4f, scale, 3);
    }

    [Fact]
    public void UltrawideDisplayUsesHeightAsLimitingDimension()
    {
        float scale = ResponsiveUiScale.Calculate(new Vector2(2560f, 1080f));

        Assert.Equal(1.2f, scale, 3);
    }

    [Fact]
    public void ScaleRemainsWithinSupportedBounds()
    {
        Assert.Equal(
            ResponsiveUiScale.MinimumScale,
            ResponsiveUiScale.Calculate(new Vector2(640f, 480f)),
            3);
        Assert.Equal(
            ResponsiveUiScale.MaximumScale,
            ResponsiveUiScale.Calculate(new Vector2(7680f, 4320f)),
            3);
    }
}
