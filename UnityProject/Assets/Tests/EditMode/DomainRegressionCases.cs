using System;
using System.Numerics;
using CrowdCheckpointSim.Core;

namespace CrowdCheckpoint.Unity.Tests
{
    // The same independent expectations run in .NET and, later, actual Unity EditMode.
    // No UnityEngine, NUnit or xUnit dependency in these fixtures.
    public static class DomainRegressionCases
    {
        public static void TimingPreservesLegacyAndPercentageFatigue()
        {
            Require(OperatorTiming.VerificationDuration(30f, 2) == 35f, "Legacy +2.5s fatigue");
            Require(OperatorTiming.VerificationDuration(30f, 2, 25f) == 45f, "Additive percentage fatigue");
            Require(OperatorTiming.VerificationDuration(30f, 100, 0f) == 30f, "Fatigue disabled");
            Require(OperatorTiming.VerificationDuration(-30f, -2, -25f) == 0f, "Negative timing clamp");
        }

        public static void CatchFatigueAndCapacityKeepTheirFloors()
        {
            Require(OperatorTiming.InterceptionSpeed(2f, 1, 20f) == 1.6f, "Catch-driven speed loss");
            Require(OperatorTiming.InterceptionSpeed(2f, 100, 20f) == 1f, "Half-speed floor");
            Require(OperatorTiming.InterceptionSpeed(2f, -1, 20f) == 2f, "Negative catches clamp");
            Require(OperatorTiming.HealthFraction(0) == 1f, "Initial capacity");
            Require(OperatorTiming.HealthFraction(100) == 0.5f, "Half-capacity floor");
        }

        public static void AcquisitionIncludesBoundaryAndRejectsInvalidRadii()
        {
            var origin = new Vector2(2f, 3f);
            Require(OperatorAcquisition.IsWithinRadius(origin, new Vector2(5f, 7f), 5f), "3-4-5 boundary");
            Require(!OperatorAcquisition.IsWithinRadius(origin, new Vector2(5f, 7.001f), 5f), "Outside boundary");
            Require(OperatorAcquisition.IsWithinRadius(origin, origin, 0f), "Zero-radius coincidence");
            foreach (float radius in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Require(!OperatorAcquisition.IsWithinRadius(origin, origin, radius), "Invalid radius rejected");
        }

        public static void QuotaMaintainsCountsAndResetsOnConfiguration()
        {
            var tracker = new DetectionQuotaTracker();
            for (int i = 0; i < 100; i++) tracker.AssignNext();
            Require(tracker.CrossingCount == 100 && tracker.GreenCount == 95 &&
                    tracker.YellowCount == 4 && tracker.RedCount == 1, "Default quota");
            tracker.Configure(0f);
            Require(tracker.CrossingCount == 0 && tracker.GreenCount == 0 &&
                    tracker.YellowCount == 0 && tracker.RedCount == 0, "Configure clears debt");
            for (int i = 0; i < 100; i++) tracker.AssignNext();
            Require(tracker.GreenCount == 99 && tracker.YellowCount == 0 && tracker.RedCount == 1,
                    "Zero Yellow quota");
            tracker.ResetInterval();
            Require(tracker.CrossingCount == 0 && tracker.GreenCount == 0 &&
                    tracker.YellowCount == 0 && tracker.RedCount == 0, "Minute reset");
        }

        public static void InvalidQuotasRemainRejected()
        {
            ExpectRangeError(() => new DetectionQuotaTracker(-0.001f));
            ExpectRangeError(() => new DetectionQuotaTracker(0.041f));
            ExpectRangeError(() => new DetectionQuotaTracker(0.04f, 0.02f));
        }

        private static void ExpectRangeError(Action action)
        {
            try { action(); }
            catch (ArgumentOutOfRangeException) { return; }
            throw new InvalidOperationException("Expected invalid quota rejection");
        }

        private static void Require(bool condition, string rule)
        {
            if (!condition) throw new InvalidOperationException("Domain regression: " + rule);
        }
    }
}
