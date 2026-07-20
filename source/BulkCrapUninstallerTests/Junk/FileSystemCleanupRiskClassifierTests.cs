using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UninstallTools;
using UninstallTools.Junk.Cleanup;
using UninstallTools.Junk.Containers;

namespace BulkCrapUninstallerTests.Junk
{
    [TestClass]
    public class FileSystemCleanupRiskClassifierTests
    {
        private const string ProtectedRoot = @"C:\Windows";
        private const string InstallRoot = @"C:\Apps\Target";

        [TestMethod]
        public void Classify_FilesystemRoot_IsCritical()
        {
            var target = CreateApplication(InstallRoot);
            var classifier = CreateClassifier(target);

            var result = classifier.Classify(CreateJunk(@"C:\", target));

            Assert.AreEqual(CleanupRiskLevel.Critical, result.Level);
        }

        [TestMethod]
        public void Classify_ProtectedRoot_IsCritical()
        {
            var target = CreateApplication(InstallRoot);
            var classifier = CreateClassifier(target);

            var result = classifier.Classify(CreateJunk(ProtectedRoot, target));

            Assert.AreEqual(CleanupRiskLevel.Critical, result.Level);
        }

        [TestMethod]
        public void Classify_AncestorOfProtectedRoot_IsCritical()
        {
            var target = CreateApplication(@"C:\");
            var classifier = CreateClassifier(target);

            var result = classifier.Classify(CreateJunk(@"C:\", target));

            Assert.AreEqual(CleanupRiskLevel.Critical, result.Level);
        }

        [TestMethod]
        public void Classify_OutsideInstallLocation_IsHighRisk()
        {
            var target = CreateApplication(InstallRoot);
            var classifier = CreateClassifier(target);

            var result = classifier.Classify(CreateJunk(@"C:\Users\Test\AppData\Roaming\Target", target));

            Assert.AreEqual(CleanupRiskLevel.High, result.Level);
        }

        [TestMethod]
        public void Classify_InstallRoot_RequiresReview()
        {
            var target = CreateApplication(InstallRoot);
            var classifier = CreateClassifier(target);

            var result = classifier.Classify(CreateJunk(InstallRoot, target));

            Assert.AreEqual(CleanupRiskLevel.Medium, result.Level);
        }

        [TestMethod]
        public void Classify_ExclusiveChildOfInstallLocation_IsLowRisk()
        {
            var target = CreateApplication(InstallRoot);
            var classifier = CreateClassifier(target);

            var result = classifier.Classify(CreateJunk(@"C:\Apps\Target\cache", target));

            Assert.AreEqual(CleanupRiskLevel.Low, result.Level);
        }

        [TestMethod]
        public void Classify_OverlappingOtherApplication_IsCritical()
        {
            var target = CreateApplication(InstallRoot);
            var other = CreateApplication(@"C:\Apps\Target\SharedComponent");
            var classifier = CreateClassifier(target, other);

            var result = classifier.Classify(CreateJunk(@"C:\Apps\Target\SharedComponent", target));

            Assert.AreEqual(CleanupRiskLevel.Critical, result.Level);
        }

        [TestMethod]
        public void Build_WithFilesystemClassifier_PreservesRiskReasons()
        {
            var target = CreateApplication(InstallRoot);
            var classifier = CreateClassifier(target);
            var plan = CleanupPlanBuilder.Build(
                new IJunkResult[] { CreateJunk(@"C:\Apps\Target\cache", target) }, classifier);

            Assert.AreEqual(CleanupRiskLevel.Low, plan.Candidates[0].Risk);
            Assert.AreEqual(1, plan.Candidates[0].RiskAssessment.Reasons.Count);
        }

        private static ApplicationUninstallerEntry CreateApplication(string installLocation)
        {
            return new ApplicationUninstallerEntry
            {
                DisplayName = "Test application",
                InstallLocation = installLocation
            };
        }

        private static FileSystemJunk CreateJunk(string path, ApplicationUninstallerEntry application)
        {
            return new FileSystemJunk(new DirectoryInfo(path), application, null);
        }

        private static FileSystemCleanupRiskClassifier CreateClassifier(
            params ApplicationUninstallerEntry[] applications)
        {
            return new FileSystemCleanupRiskClassifier(applications, new[] { ProtectedRoot });
        }
    }
}
