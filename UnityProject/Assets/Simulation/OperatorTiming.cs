// Adapted from Core/OperatorTiming.cs; preserve reference behaviour.
using System;
namespace CrowdCheckpointSim.Core
{

    /// <summary>Reference timing values for operator intervention, verification and visible workload.</summary>
    public static class OperatorTiming
    {
        public const float CooperativeStopDistance = 2.0f;
        public const float CatchDistance = 0.50f;
        public const float PostRadius = 0.40f;
        public const float StopSeconds = 1.20f;
        public const float VerificationFatigueStepSeconds = 2.5f;
        public const float MaxWalkingSpeed = 1.55f;
        public const float EscortSpeed = 1.10f;
        public const float YieldInfluenceDistance = 1.65f;
        public const float YieldCorridorHalfWidth = 0.50f;
        public const float YieldSideStepDistance = 0.42f;

        // The wider/longer envelope is used for Purple right-of-way whenever an operator is away
        // from its post. During escort it also covers the caught pedestrian as a two-person formation.
        public const float EscortYieldInfluenceDistance = 2.30f;
        public const float EscortYieldCorridorHalfWidth = 0.82f;
        public const float EscortYieldSideStepDistance = 0.62f;
        public const float EscortYieldLateralStrength = 3.20f;
        public const float EscortYieldBrakingGain = 3.00f;

        public const float DirectBlockDistance = 0.72f;
        public const float DirectBlockHalfWidth = 0.38f;
        public const float MinimumYieldSideClearance = 0.035f;

        /// <summary>Each completed manual enrolment removes ten percentage points from the bar.</summary>
        public const float HealthLossPerCompletedVerification = 0.10f;

        /// <summary>The visible operator health/workload bar never falls below 50%.</summary>
        public const float MinimumHealthFraction = 0.50f;

        /// <summary>
        /// Legacy two-argument API: add the original fixed 2.5 seconds per completed verification.
        /// Current simulation operators use the configurable three-argument overload.
        /// </summary>
        public static float VerificationDuration(
            float initialVerificationSeconds,
            int completedVerificationCount)
        {
            int completed = Math.Max(0, completedVerificationCount);
            float initial = Math.Max(0.0f, initialVerificationSeconds);
            return initial + completed * VerificationFatigueStepSeconds;
        }

        /// <summary>
        /// Add a configurable percentage of the initial duration per completed case. This is an
        /// additive increase, not compounded interest; zero disables timing fatigue.
        /// </summary>
        public static float VerificationDuration(
            float initialVerificationSeconds,
            int completedVerificationCount,
            float fatigueIncreasePercent)
        {
            int completed = Math.Max(0, completedVerificationCount);
            float initial = Math.Max(0f, initialVerificationSeconds);
            float increase = Math.Max(0f, fatigueIncreasePercent) / 100f;
            return initial + completed * initial * increase;
        }

        /// <summary>
        /// Reduce normal pursuit by a percentage of its initial speed after each catch. The minimum
        /// is exactly half the initial speed, including when a decrement would cross that floor.
        /// </summary>
        public static float InterceptionSpeed(float initialSpeed, int catchCount, float lossPercent)
        {
            float remaining = Math.Max(0.5f, 1f - Math.Max(0, catchCount) * Math.Max(0f, lossPercent) / 100f);
            return initialSpeed * remaining;
        }

        /// <summary>
        /// Remaining visible operator health/work-capacity fraction. It starts at 1.00, drops by
        /// 0.10 after each completed manual verification, and is capped at 0.50 minimum.
        /// </summary>
        public static float HealthFraction(int completedVerificationCount)
        {
            int completed = Math.Max(0, completedVerificationCount);
            return Math.Max(
                MinimumHealthFraction,
                1.0f - completed * HealthLossPerCompletedVerification);
        }
    }
}
