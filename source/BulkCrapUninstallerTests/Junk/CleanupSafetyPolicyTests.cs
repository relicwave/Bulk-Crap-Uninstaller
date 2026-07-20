using Microsoft.VisualStudio.TestTools.UnitTesting;
using UninstallTools.Junk.Cleanup;
using UninstallTools.Junk.Confidence;

namespace BulkCrapUninstallerTests.Junk
{
    [TestClass]
    public class CleanupSafetyPolicyTests
    {
        [TestMethod]
        public void Evaluate_VeryGoodConfidenceAndLowRisk_IsAutomatic()
        {
            var result = CleanupSafetyPolicy.Evaluate(ConfidenceLevel.VeryGood, CleanupRiskLevel.Low);

            Assert.AreEqual(CleanupSelection.Automatic, result);
        }

        [DataTestMethod]
        [DataRow(ConfidenceLevel.Unknown)]
        [DataRow(ConfidenceLevel.Questionable)]
        [DataRow(ConfidenceLevel.Good)]
        public void Evaluate_LowRiskWithoutVeryGoodConfidence_RequiresReview(ConfidenceLevel confidence)
        {
            var result = CleanupSafetyPolicy.Evaluate(confidence, CleanupRiskLevel.Low);

            Assert.AreEqual(CleanupSelection.ReviewRequired, result);
        }

        [DataTestMethod]
        [DataRow(CleanupRiskLevel.Medium)]
        [DataRow(CleanupRiskLevel.High)]
        public void Evaluate_NonLowRisk_RequiresReview(CleanupRiskLevel risk)
        {
            var result = CleanupSafetyPolicy.Evaluate(ConfidenceLevel.VeryGood, risk);

            Assert.AreEqual(CleanupSelection.ReviewRequired, result);
        }

        [TestMethod]
        public void Evaluate_CriticalRisk_IsBlocked()
        {
            var result = CleanupSafetyPolicy.Evaluate(ConfidenceLevel.VeryGood, CleanupRiskLevel.Critical);

            Assert.AreEqual(CleanupSelection.Blocked, result);
        }

        [TestMethod]
        public void Evaluate_BadConfidence_IsBlocked()
        {
            var result = CleanupSafetyPolicy.Evaluate(ConfidenceLevel.Bad, CleanupRiskLevel.Low);

            Assert.AreEqual(CleanupSelection.Blocked, result);
        }
    }
}
