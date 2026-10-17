using System.Numerics;
using CrowdCheckpointSim.Rendering;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class HumanoidAnimationTests
{
    [Fact]
    public void StationaryAgentUsesNeutralPose()
    {
        HumanoidAnimationPose pose = HumanoidAnimation.Compute(
            12.5f,
            7,
            Vector2.Zero,
            forceRunning: false);

        Assert.False(pose.IsMoving);
        Assert.False(pose.IsRunning);
        Assert.Equal(0f, pose.BobHeight, 4);
        Assert.Equal(0f, pose.LeanDistance, 4);
        Assert.Equal(0f, pose.LeftLegSwing, 4);
        Assert.Equal(0f, pose.RightLegSwing, 4);
        Assert.Equal(0f, pose.LeftArmSwing, 4);
        Assert.Equal(0f, pose.RightArmSwing, 4);
    }

    [Fact]
    public void WalkingUsesContralateralArmAndLegMotion()
    {
        HumanoidAnimationPose pose = HumanoidAnimation.Compute(
            0.37f,
            3,
            new Vector2(-1.2f, 0.1f),
            forceRunning: false);

        Assert.True(pose.IsMoving);
        Assert.False(pose.IsRunning);
        Assert.True(MathF.Abs(pose.LeftLegSwing) > 0.001f);
        Assert.True(MathF.Abs(pose.LeftArmSwing) > 0.001f);
        Assert.True(pose.LeftLegSwing * pose.LeftArmSwing < 0f);
        Assert.True(pose.RightLegSwing * pose.RightArmSwing < 0f);
    }

    [Fact]
    public void ForcedRunningAddsForwardLean()
    {
        HumanoidAnimationPose walk = HumanoidAnimation.Compute(
            0.42f,
            11,
            new Vector2(-1.1f, 0f),
            forceRunning: false);
        HumanoidAnimationPose run = HumanoidAnimation.Compute(
            0.42f,
            11,
            new Vector2(-1.1f, 0f),
            forceRunning: true);

        Assert.False(walk.IsRunning);
        Assert.True(run.IsRunning);
        Assert.True(run.LeanDistance > walk.LeanDistance);
    }

    [Fact]
    public void PoseIsDeterministicForSameInputs()
    {
        HumanoidAnimationPose first = HumanoidAnimation.Compute(
            9.75f,
            42,
            new Vector2(-2.5f, 0.2f),
            forceRunning: true);
        HumanoidAnimationPose second = HumanoidAnimation.Compute(
            9.75f,
            42,
            new Vector2(-2.5f, 0.2f),
            forceRunning: true);

        Assert.Equal(first, second);
    }
}
