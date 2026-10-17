extern alias reference;

using System;
using System.Linq;
using System.Numerics;
using CrowdCheckpoint.Unity.Tests;
using CrowdCheckpointSim.Core;
using Xunit;
using Original = reference::CrowdCheckpointSim.Core;

namespace CrowdCheckpoint.Portability.Tests
{
    public sealed class DomainParityTests
    {
        [Fact]
        public void LegacyAndPercentageFatigueExpectations() =>
            DomainRegressionCases.TimingPreservesLegacyAndPercentageFatigue();

        [Fact]
        public void SpeedAndCapacityFloorExpectations() =>
            DomainRegressionCases.CatchFatigueAndCapacityKeepTheirFloors();

        [Fact]
        public void AcquisitionBoundaryExpectations() =>
            DomainRegressionCases.AcquisitionIncludesBoundaryAndRejectsInvalidRadii();

        [Fact]
        public void QuotaResetExpectations() =>
            DomainRegressionCases.QuotaMaintainsCountsAndResetsOnConfiguration();

        [Fact]
        public void QuotaRejectionExpectations() => DomainRegressionCases.InvalidQuotasRemainRejected();

        [Theory]
        [InlineData(0f)]
        [InlineData(0.01f)]
        [InlineData(0.025f)]
        [InlineData(0.04f)]
        public void EveryQuotaDecisionAndCounterMatchesReferenceAcrossReset(float yellow)
        {
            var expected = new Original.DetectionQuotaTracker(yellow);
            var actual = new DetectionQuotaTracker(yellow);
            for (int i = 0; i < 1000; i++)
            {
                if (i == 411) { expected.ResetInterval(); actual.ResetInterval(); }
                if (i == 733) { expected.Configure(0f); actual.Configure(0f); }
                Assert.Equal((int)expected.AssignNext(), (int)actual.AssignNext());
                Assert.Equal(expected.CrossingCount, actual.CrossingCount);
                Assert.Equal(expected.GreenCount, actual.GreenCount);
                Assert.Equal(expected.YellowCount, actual.YellowCount);
                Assert.Equal(expected.RedCount, actual.RedCount);
                SameFloat(expected.GreenFraction, actual.GreenFraction);
                SameFloat(expected.YellowFraction, actual.YellowFraction);
                SameFloat(expected.RedFraction, actual.RedFraction);
            }
        }

        [Theory]
        [InlineData(10f, 0, 25f)]
        [InlineData(30f, 2, 25f)]
        [InlineData(180f, 200, 100f)]
        [InlineData(10f, 10, 0f)]
        [InlineData(-10f, -2, -25f)]
        [InlineData(13.75f, 7, 33.33f)]
        public void TimingAndSpeedBitsMatchReference(float initial, int count, float percent)
        {
            SameFloat(Original.OperatorTiming.VerificationDuration(initial, count),
                OperatorTiming.VerificationDuration(initial, count));
            SameFloat(Original.OperatorTiming.VerificationDuration(initial, count, percent),
                OperatorTiming.VerificationDuration(initial, count, percent));
            SameFloat(Original.OperatorTiming.InterceptionSpeed(initial, count, percent),
                OperatorTiming.InterceptionSpeed(initial, count, percent));
            SameFloat(Original.OperatorTiming.HealthFraction(count), OperatorTiming.HealthFraction(count));
        }

        [Theory]
        [InlineData(5f, 5f)]
        [InlineData(5.001f, 5f)]
        [InlineData(0f, 0f)]
        [InlineData(0f, -1f)]
        [InlineData(0f, float.NaN)]
        [InlineData(0f, float.PositiveInfinity)]
        [InlineData(0f, float.NegativeInfinity)]
        [InlineData(float.NaN, 5f)]
        [InlineData(float.PositiveInfinity, 5f)]
        public void AcquisitionMatchesReferenceForBoundaryAndNonFiniteInputs(float distance, float radius)
        {
            var origin = new Vector2(2f, 3f);
            var target = origin + new Vector2(distance, 0f);
            Assert.Equal(Original.OperatorAcquisition.IsWithinRadius(origin, target, radius),
                OperatorAcquisition.IsWithinRadius(origin, target, radius));
        }

        [Fact]
        public void AllEnumNamesValuesAndTimingConstantsMatchReference()
        {
            var referenceAssembly = typeof(Original.DetectionStatus).Assembly;
            foreach (var type in typeof(DetectionStatus).Assembly.GetTypes().Where(t => t.IsEnum))
            {
                var original = referenceAssembly.GetType(type.FullName!, throwOnError: true)!;
                Assert.Equal(Enum.GetNames(original), Enum.GetNames(type));
                Assert.Equal(Enum.GetValues(original).Cast<object>().Select(Convert.ToInt32),
                    Enum.GetValues(type).Cast<object>().Select(Convert.ToInt32));
            }
            foreach (var field in typeof(OperatorTiming).GetFields().Where(f => f.IsLiteral))
                Assert.Equal(typeof(Original.OperatorTiming).GetField(field.Name)!.GetRawConstantValue(),
                    field.GetRawConstantValue());
        }

        [Theory]
        [InlineData(-0.01f, 0.01f)]
        [InlineData(0.05f, 0.01f)]
        [InlineData(0.04f, 0.02f)]
        [InlineData(float.NaN, 0.01f)]
        [InlineData(0.04f, float.NaN)]
        public void QuotaValidationPreservesReferenceIncludingExistingNaNAcceptance(float yellow, float red)
        {
            var expected = Record.Exception(() => new Original.DetectionQuotaTracker(yellow, red));
            var actual = Record.Exception(() => new DetectionQuotaTracker(yellow, red));
            if (float.IsNaN(yellow) || float.IsNaN(red))
            {
                Assert.Null(expected);
                Assert.Null(actual);
            }
            else
            {
                Assert.IsType<ArgumentOutOfRangeException>(expected);
                Assert.IsType<ArgumentOutOfRangeException>(actual);
            }
            Assert.Equal(expected?.GetType(), actual?.GetType());
            Assert.Equal(expected?.Message, actual?.Message);
        }

        private static void SameFloat(float expected, float actual) =>
            Assert.Equal(BitConverter.SingleToInt32Bits(expected), BitConverter.SingleToInt32Bits(actual));
    }
}
