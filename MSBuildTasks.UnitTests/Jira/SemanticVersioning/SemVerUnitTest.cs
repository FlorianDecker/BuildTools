using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Remotion.BuildTools.MSBuildTasks.Jira.SemanticVersioning;

namespace BuildTools.MSBuildTasks.UnitTests.Jira.SemanticVersioning
{
    [TestFixture]
    class SemVerUnitTest
    {
        private SemanticVersionParser _semanticVersionParser;

        [SetUp]
        public void SetUp()
        {
            _semanticVersionParser = new SemanticVersionParser();
        }

        [Test]
        public void SemVer_WithourPre_ShouldParse()
        {
            string version = "1.2.3";
            Remotion.BuildTools.MSBuildTasks.Jira.SemanticVersioning.SemanticVersion semver = _semanticVersionParser.ParseVersion(version);

            Assert.AreEqual(1, semver.Major);
            Assert.AreEqual(2, semver.Minor);
            Assert.AreEqual(3, semver.Patch);

            Assert.IsNull(semver.Pre);
            Assert.IsNull(semver.PreReleaseCounter);
        }

        [Test]
        public void SemVer_WithPre_ShouldParse()
        {
            string version = "1.2.3-alpha.4";
            Remotion.BuildTools.MSBuildTasks.Jira.SemanticVersioning.SemanticVersion semver = _semanticVersionParser.ParseVersion(version);

            Assert.AreEqual(1, semver.Major);
            Assert.AreEqual(2, semver.Minor);
            Assert.AreEqual(3, semver.Patch);
            
            Assert.AreEqual(PreReleaseStage.alpha, semver.Pre);
            Assert.AreEqual(4, semver.PreReleaseCounter);
        }

        [Test]
        public void SemVer_InvalidFormat_ShouldThrowArgumentException()
        {
            Assert.That(
              () => _semanticVersionParser.ParseVersion("TotalInvalidFormat"), 
              Throws.ArgumentException.With.Message.EqualTo("Version has an invalid format. Expected equivalent to '1.2.3' or '1.2.3-alpha.4'"));

            Assert.That(
              () => _semanticVersionParser.ParseVersion("1.2.3-invalid.4"),
              Throws.ArgumentException.With.Message.EqualTo("Version has an invalid format. Expected equivalent to '1.2.3' or '1.2.3-alpha.4'"));

            Assert.That(
              () => _semanticVersionParser.ParseVersion("1.2.3.4"),
              Throws.ArgumentException.With.Message.EqualTo("Version has an invalid format. Expected equivalent to '1.2.3' or '1.2.3-alpha.4'"));

            Assert.That(
              () => _semanticVersionParser.ParseVersion("1.2.3.alpha-4"),
              Throws.ArgumentException.With.Message.EqualTo("Version has an invalid format. Expected equivalent to '1.2.3' or '1.2.3-alpha.4'"));
        }


        [Test]
        public void SemVer_Ordering()
        {
            var semVer1 = _semanticVersionParser.ParseVersion("1.2.3");
            var semVer2 = _semanticVersionParser.ParseVersion("1.2.4");
            var semVer3 = _semanticVersionParser.ParseVersion("1.3.0");
            var semVer4 = _semanticVersionParser.ParseVersion("1.4.0-alpha.1");
            var semVer5 = _semanticVersionParser.ParseVersion("1.4.0-beta.1");
            var semVer6 = _semanticVersionParser.ParseVersion("1.4.0-beta.2");
            var semVer7 = _semanticVersionParser.ParseVersion("1.4.0-rc.1");
            var semVer8 = _semanticVersionParser.ParseVersion("1.4.0");
            var semVer9 = _semanticVersionParser.ParseVersion("2.0.0");

            var semVerList = new List<Remotion.BuildTools.MSBuildTasks.Jira.SemanticVersioning.SemanticVersion>() {semVer9, semVer8, semVer7, semVer6, semVer5, semVer4, semVer3, semVer2, semVer1};

            var orderedList = semVerList.OrderBy(x => x).ToList();

          Assert.That(
            orderedList, Is.EqualTo(new List<Remotion.BuildTools.MSBuildTasks.Jira.SemanticVersioning.SemanticVersion>()
            {
              semVer1,
              semVer2,
              semVer3,
              semVer4,
              semVer5,
              semVer6,
              semVer7,
              semVer8,
              semVer9
            }));
        }
    }
}
