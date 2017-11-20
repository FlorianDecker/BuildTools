using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Remotion.BuildTools.MSBuildTasks.Jira.SemanticVersioning;
using Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeImplementations;
using Remotion.BuildTools.MSBuildTasks.Jira.Utility;

namespace BuildTools.MSBuildTasks.UnitTests.Jira
{
  [TestFixture]
  public class JiraVersionMovePositionerTest
  {
    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_OnEmptyList_ReturnsFalse ()
    {
      var jiraProjectVersions = new List<JiraProjectVersionComparableAdapter<SemanticVersion>>();
      var createdVersion = CreateSemanticVersion ("1.0.0");

      var jiraVersionMovePositioner = new JiraVersionMovePositioner<SemanticVersion> (jiraProjectVersions, createdVersion);

      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.False);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_WhenHigherVersionIsBefore_ReturnsTrue ()
    {
      var jiraProjectVersions = CreateSemanticJiraProjectVersions ("1.0.1");
      var createdVersion = CreateSemanticVersion ("1.0.0");

      var jiraVersionMovePositioner = new JiraVersionMovePositioner<SemanticVersion> (jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.True);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_WhenLowerVersionIsBefore_ReturnsFalse ()
    {
      var jiraProjectVersions = CreateSemanticJiraProjectVersions ("1.0.0");
      var createdVersion = CreateSemanticVersion ("1.0.1");

      var jiraVersionMovePositioner = new JiraVersionMovePositioner<SemanticVersion> (jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.False);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_WithPrereleaseVersion_ReturnsTrue ()
    {
      var jiraProjectVersions = CreateSemanticJiraProjectVersions ("2.1.3", "2.2.0");

      var createdVersion = CreateSemanticVersion ("2.2.0-alpha.5");

      var jiraVersionMovePositioner = new JiraVersionMovePositioner<SemanticVersion> (jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.True);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_WithInvalidVersionInList_ReturnsFalse ()
    {
      var jiraProjectVersions = CreateSemanticJiraProjectVersions (null, "2.1.3");

      var createdVersion = CreateSemanticVersion ("2.2.0");

      var jiraVersionMovePositioner = new JiraVersionMovePositioner<SemanticVersion> (jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.False);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_WithInvalidVersionInList_ReturnsTrue ()
    {
      var jiraProjectVersions = CreateSemanticJiraProjectVersions (null, "2.1.3");
      var createdVersion = CreateSemanticVersion ("2.1.0");

      var jiraVersionMovePositioner = new JiraVersionMovePositioner<SemanticVersion> (jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.True);
    }

    [Test]
    public void JiraVersionMovePositioner_HasToBeMoved_GivenListNotInOrder_ReturnsTrue ()
    {
      var jiraProjectVersions = CreateSemanticJiraProjectVersions ("2.0.1", "1.9.3");

      var createdVersion = CreateSemanticVersion ("2.0.2");

      var jiraVersionMovePositioner = new JiraVersionMovePositioner<SemanticVersion> (jiraProjectVersions, createdVersion);
      Assert.That (jiraVersionMovePositioner.HasToBeMoved(), Is.True);
    }

    [Test]
    public void JiraVersionMovePositioner_GetVersionBeforeCreatedVersion_WithPrereleaseVersion_ReturnsCorrectVersion ()
    {
      var jiraProjectVersions = CreateSemanticJiraProjectVersions ("2.1.3", "2.2.0");

      var createdVersion = CreateSemanticVersion ("2.2.0-alpha.5");

      var jiraVersionMovePositioner = new JiraVersionMovePositioner<SemanticVersion> (jiraProjectVersions, createdVersion);
      var versionBeforeCreated = jiraVersionMovePositioner.GetVersionBeforeCreatedVersion();

      Assert.That (versionBeforeCreated.ComparableVersion.ToString(), Is.EqualTo ("2.1.3"));
    }

    [Test]
    public void JiraVersionMovePositioner_GetVersionBeforeCreatedVersion_WithOnlyMinorDifference_ReturnsCorrectVersion ()
    {
      var jiraProjectVersions = CreateSemanticJiraProjectVersions ("1.8.1", "1.8.3");
      var createdVersion = CreateSemanticVersion ("1.8.2");

      var jiraVersionMovePositioner = new JiraVersionMovePositioner<SemanticVersion> (jiraProjectVersions, createdVersion);
      var versionCreatedBefore = jiraVersionMovePositioner.GetVersionBeforeCreatedVersion();

      Assert.That (versionCreatedBefore.ComparableVersion.ToString(), Is.EqualTo ("1.8.1"));
    }

    [Test]
    public void JiraVersionMovePositioner_GetVersionBeforeCreatedVersion_WouldBeMovedToFirstPosition_ReturnsNull ()
    {
      var jiraProjectVersions = CreateSemanticJiraProjectVersions ("1.0.1");

      var createdVersion = CreateSemanticVersion ("1.0.0");

      var jiraVersionMovePositioner = new JiraVersionMovePositioner<SemanticVersion> (jiraProjectVersions, createdVersion);
      var versionCreatedBefore = jiraVersionMovePositioner.GetVersionBeforeCreatedVersion();

      Assert.That (versionCreatedBefore, Is.Null);
    }

    private JiraProjectVersionComparableAdapter<SemanticVersion> CreateSemanticVersion (string version)
    {
      var semanticVersionParser = new SemanticVersionParser();
      return new JiraProjectVersionComparableAdapter<SemanticVersion> { ComparableVersion = semanticVersionParser.ParseVersion (version) };
    }

    private List<JiraProjectVersionComparableAdapter<SemanticVersion>> CreateSemanticJiraProjectVersions (params string[] versions)
    {
      var jiraProjectVersionComparableAdapterList = new List<JiraProjectVersionComparableAdapter<SemanticVersion>>();
      var semanticVersionParser = new SemanticVersionParser();

      foreach (var version in versions)
      {
        jiraProjectVersionComparableAdapterList.Add (
            new JiraProjectVersionComparableAdapter<SemanticVersion>()
            {
              JiraProjectVersion = new JiraProjectVersion() { name = version },
              ComparableVersion = version == null ? null : semanticVersionParser.ParseVersion (version)
            });
      }

      return jiraProjectVersionComparableAdapterList;
    }

  }
}