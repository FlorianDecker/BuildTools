using System;
using System.Collections.Generic;
using NUnit.Framework;
using Remotion.BuildTools.MSBuildTasks.Jira.SemanticVersioning;
using Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeImplementations;
using Remotion.BuildTools.MSBuildTasks.Jira.Utility;

namespace BuildTools.MSBuildTasks.UnitTests
{
  [TestFixture]
  public class JiraVersionMovePositionerTest
  {
    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_OnEmptyList_ReturnsFalse ()
    {
      var jiraProjectVersions = new List<JiraProjectVersion>();
      var createdVersion = new JiraProjectVersion() { name = "1.0.0" };

      var jiraVersionMovePositioner = new JiraVersionMovePositioner (jiraProjectVersions, createdVersion);

      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.False);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_WhenHigherVersionIsBefore_ReturnsTrue ()
    {
      var jiraProjectVersions = new List<JiraProjectVersion>
                                {
                                  new JiraProjectVersion { name = "1.0.1" }
                                };

      var createdVersion = new JiraProjectVersion() { name = "1.0.0" };

      var jiraVersionMovePositioner = new JiraVersionMovePositioner (jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.True);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_WhenLowerVersionIsBefore_ReturnsFalse ()
    {
      var jiraProjectVersions = new List<JiraProjectVersion>
                                {
                                  new JiraProjectVersion { name = "1.0.0" }
                                };

      var createdVersion = new JiraProjectVersion() { name = "1.0.1" };

      var jiraVersionMovePositioner = new JiraVersionMovePositioner (jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.False);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_WithPrereleaseVersion_ReturnsTrue ()
    {
      var jiraProjectVersions = new List<JiraProjectVersion>
                                {
                                  new JiraProjectVersion { name = "2.1.3" },
                                  new JiraProjectVersion { name = "2.2.0" }
                                };

      var createdVersion = new JiraProjectVersion() { name = "2.2.0-alpha.5" };

      var jiraVersionMovePositioner = new JiraVersionMovePositioner(jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.True);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_WithInvalidVersionInList_ReturnsFalse ()
    {
      var jiraProjectVersions = new List<JiraProjectVersion>
                                {
                                  new JiraProjectVersion { name = "asd" },
                                  new JiraProjectVersion { name = "2.1.3" }
                                };

      var createdVersion = new JiraProjectVersion() { name = "2.2.0" };

      var jiraVersionMovePositioner = new JiraVersionMovePositioner(jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.False);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_WithInvalidVersionInList_ReturnsTrue ()
    {
      var jiraProjectVersions = new List<JiraProjectVersion>
                                {
                                  new JiraProjectVersion { name = "asd" },
                                  new JiraProjectVersion { name = "2.1.3" }
                                };

      var createdVersion = new JiraProjectVersion() { name = "2.1.0" };

      var jiraVersionMovePositioner = new JiraVersionMovePositioner(jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.True);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_GivenListNotInOrder_ReturnsTrue ()
    {
      var jiraProjectVersions = new List<JiraProjectVersion>
                                {
                                  new JiraProjectVersion { name = "2.0.1" },
                                  new JiraProjectVersion { name = "1.9.3" }
                                };

      var createdVersion = new JiraProjectVersion() { name = "2.0.2" };

      var jiraVersionMovePositioner = new JiraVersionMovePositioner(jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.True);
    }

    [Test]
    public void JiraVersionMovePositioner_GetVersionBeforeCreatedVersion_WithPrereleaseVersion_ReturnsCorrectVersion ()
    {
      var jiraProjectVersions = new List<JiraProjectVersion>
                                {
                                  new JiraProjectVersion { name = "2.1.3" },
                                  new JiraProjectVersion { name = "2.2.0" }
                                };

      var createdVersion = new JiraProjectVersion() { name = "2.2.0-alpha.5" };

      var jiraVersionMovePositioner = new JiraVersionMovePositioner(jiraProjectVersions, createdVersion);
      var versionBeforeCreated = jiraVersionMovePositioner.GetVersionBeforeCreatedVersion();

      Assert.That (versionBeforeCreated.SemanticVersion.ToString(), Is.EqualTo ("2.1.3"));
    }

    [Test]
    public void JiraVersionMovePositioner_GetVersionBeforeCreatedVersion_WithOnlyMinorDifference_ReturnsCorrectVersion ()
    {
      var jiraProjectVersions = new List<JiraProjectVersion>
                                {
                                  new JiraProjectVersion { name = "1.8.1" },
                                  new JiraProjectVersion { name = "1.8.3" }
                                };

      var createdVersion = new JiraProjectVersion() { name = "1.8.2" };

      var jiraVersionMovePositioner = new JiraVersionMovePositioner(jiraProjectVersions, createdVersion);
      var versionCreatedBefore = jiraVersionMovePositioner.GetVersionBeforeCreatedVersion();

      Assert.That (versionCreatedBefore.SemanticVersion.ToString(), Is.EqualTo ("1.8.1"));
    }

    [Test]
    public void JiraVersionMovePositioner_GetVersionBeforeCreatedVersion_WouldBeMovedToFirstPosition_ReturnsNull ()
    {
      var jiraProjectVersions = new List<JiraProjectVersion>
                                {
                                  new JiraProjectVersion { name = "1.0.1" }
                                };

      var createdVersion = new JiraProjectVersion() { name = "1.0.0" };

      var jiraVersionMovePositioner = new JiraVersionMovePositioner(jiraProjectVersions, createdVersion);
      var versionCreatedBefore = jiraVersionMovePositioner.GetVersionBeforeCreatedVersion();

      Assert.That (versionCreatedBefore, Is.Null);
    }
  }
}
