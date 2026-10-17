using NUnit.Framework;

namespace CrowdCheckpoint.Unity.Tests
{
    public sealed class DomainHelperTests
    {
        [Test]
        public void TimingPreservesLegacyAndPercentageFatigue() =>
            DomainRegressionCases.TimingPreservesLegacyAndPercentageFatigue();

        [Test]
        public void CatchFatigueAndCapacityKeepTheirFloors() =>
            DomainRegressionCases.CatchFatigueAndCapacityKeepTheirFloors();

        [Test]
        public void AcquisitionIncludesBoundaryAndRejectsInvalidRadii() =>
            DomainRegressionCases.AcquisitionIncludesBoundaryAndRejectsInvalidRadii();

        [Test]
        public void QuotaMaintainsCountsAndResetsOnConfiguration() =>
            DomainRegressionCases.QuotaMaintainsCountsAndResetsOnConfiguration();

        [Test]
        public void InvalidQuotasRemainRejected() =>
            DomainRegressionCases.InvalidQuotasRemainRejected();
    }
}
