using System.Numerics;

namespace CrowdCheckpointSim.Rendering;

/// <summary>
/// Presentation-only procedural gait model for the 2.5D humanoids.
/// It never changes simulation positions, velocities, collision radii, SFM forces,
/// catch distances, acquisition distances, or behavioural state machines.
/// </summary>
public static class HumanoidAnimation
{
    private const float TwoPi = MathF.PI * 2f;
    private const float MovingThreshold = 0.05f;

    /// <summary>
    /// Build a deterministic visual pose from simulation time and the agent's current velocity.
    /// </summary>
    public static HumanoidAnimationPose Compute(
        float timeSeconds,
        int animationSeed,
        Vector2 velocity,
        bool forceRunning)
    {
        float speed = velocity.Length();
        bool isMoving = speed > MovingThreshold;

        Vector2 forward = isMoving
            ? Vector2.Normalize(velocity)
            : new Vector2(-1f, 0f);
        Vector2 side = new(-forward.Y, forward.X);

        if (!isMoving)
        {
            return new HumanoidAnimationPose(
                forward,
                side,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                false);
        }

        bool isRunning = forceRunning || speed >= 1.90f;
        float referenceSpeed = isRunning ? 2.50f : 1.25f;
        float motionScale = Math.Clamp(speed / referenceSpeed, 0.32f, 1.15f);

        // Walking cadence varies slightly with speed. Running uses a deliberately faster cadence.
        float cadenceHz = isRunning
            ? 2.35f
            : Math.Clamp(1.28f + speed * 0.34f, 1.30f, 1.82f);
        float phase = timeSeconds * cadenceHz * TwoPi + animationSeed * 0.731f;
        float stride = MathF.Sin(phase);

        float legAmplitude = (isRunning ? 0.285f : 0.155f) * motionScale;
        float armAmplitude = (isRunning ? 0.235f : 0.125f) * motionScale;
        float kneeAmplitude = (isRunning ? 0.115f : 0.052f) * motionScale;
        float bobAmplitude = (isRunning ? 0.032f : 0.015f) * motionScale;

        float leftLegSwing = stride * legAmplitude;
        float rightLegSwing = -leftLegSwing;

        // Contralateral gait: the arm opposite the forward leg swings forward.
        float leftArmSwing = -stride * armAmplitude;
        float rightArmSwing = -leftArmSwing;

        // The forward/swing leg receives more knee and foot lift than the stance leg.
        float leftKneeLift = MathF.Max(0f, stride) * kneeAmplitude;
        float rightKneeLift = MathF.Max(0f, -stride) * kneeAmplitude;

        // Two restrained vertical pulses per gait cycle.
        float bobHeight = (0.5f - 0.5f * MathF.Cos(phase * 2f)) * bobAmplitude;
        float leanDistance = isRunning ? 0.060f * motionScale : 0.008f * motionScale;

        return new HumanoidAnimationPose(
            forward,
            side,
            bobHeight,
            leanDistance,
            leftLegSwing,
            rightLegSwing,
            leftKneeLift,
            rightKneeLift,
            leftArmSwing,
            rightArmSwing,
            true,
            isRunning);
    }
}

/// <summary>Small immutable set of presentation-only gait parameters.</summary>
public readonly record struct HumanoidAnimationPose(
    Vector2 Forward,
    Vector2 Side,
    float BobHeight,
    float LeanDistance,
    float LeftLegSwing,
    float RightLegSwing,
    float LeftKneeLift,
    float RightKneeLift,
    float LeftArmSwing,
    float RightArmSwing,
    bool IsMoving,
    bool IsRunning);
