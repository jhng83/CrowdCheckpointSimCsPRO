using CrowdCheckpointSim.Rendering;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class IsometricCameraTests
{
    [Fact]
    public void AdjustZoom_ZoomsInAndOutAndClampsToSafeRange()
    {
        IsometricCamera camera = new();

        camera.AdjustZoom(1f);
        Assert.True(camera.Zoom > 1f);

        camera.AdjustZoom(-1f);
        Assert.InRange(camera.Zoom, 0.99f, 1.01f);

        camera.AdjustZoom(100f);
        Assert.Equal(IsometricCamera.MaxZoom, camera.Zoom, 3);

        camera.AdjustZoom(-100f);
        Assert.Equal(IsometricCamera.MinZoom, camera.Zoom, 3);
    }

    [Fact]
    public void SetZoom_UsesDirectValueAndClampsToSafeRange()
    {
        IsometricCamera camera = new();

        camera.SetZoom(1.35f);
        Assert.Equal(1.35f, camera.Zoom, 3);

        camera.SetZoom(99f);
        Assert.Equal(IsometricCamera.MaxZoom, camera.Zoom, 3);

        camera.SetZoom(-99f);
        Assert.Equal(IsometricCamera.MinZoom, camera.Zoom, 3);
    }
}
