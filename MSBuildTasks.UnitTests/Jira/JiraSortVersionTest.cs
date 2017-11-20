using System;
using NUnit.Framework;
using Remotion.BuildTools.MSBuildTasks.Jira.SemanticVersioning;
using Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeImplementations;
using Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeInterfaces;
using Remotion.BuildTools.MSBuildTasks.Jira.Utility;
using Rhino.Mocks;

namespace BuildTools.MSBuildTasks.UnitTests.Jira
{
  public class JiraSortVersionTest
  {
    [Test]
    public void SortVersion_NoMethodCalledWhenHasToBeMovedReturnsFalse ()
    {
      var stubbedJiraService = MockRepository.Mock<IJiraProjectVersionService>();
      var stubbedJiraMovePositioner = MockRepository.Mock<IJiraVersionMovePositioner<SemanticVersion>>();

      stubbedJiraMovePositioner.Stub (x => x.HasToBeMoved()).Return (false);

      var jiraSortVersion = new JiraSortVersion<SemanticVersion> (stubbedJiraService, stubbedJiraMovePositioner);

      jiraSortVersion.SortVersion();

      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersion (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersionByPosition (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
    }

    [Test]
    public void SortVersion_MoveVersionByPositionCalledWhenGetVersionBeforeCreatedVersionReturnsNull ()
    {
      var stubbedJiraService = MockRepository.Mock<IJiraProjectVersionService>();
      var stubbedJiraMovePositioner = MockRepository.Mock<IJiraVersionMovePositioner<SemanticVersion>>();
      var stubbedJiraProjectVersion =
          new JiraProjectVersionComparableAdapter<SemanticVersion>() { JiraProjectVersion = new JiraProjectVersion() { id = "someID" } };

      stubbedJiraMovePositioner.Stub (x => x.HasToBeMoved()).Return (true);
      stubbedJiraMovePositioner.Stub (x => x.GetVersionBeforeCreatedVersion()).Return (null);
      stubbedJiraMovePositioner.Stub (x => x.GetCreatedVersion()).Return (stubbedJiraProjectVersion);

      var jiraSortVersion = new JiraSortVersion<SemanticVersion> (stubbedJiraService, stubbedJiraMovePositioner);

      jiraSortVersion.SortVersion();

      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersion (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
      stubbedJiraService.AssertWasCalled (
          x => x.MoveVersionByPosition (Arg<string>.Is.Equal (stubbedJiraProjectVersion.JiraProjectVersion.id), Arg<string>.Is.Equal ("First")));
    }

    [Test]
    public void SortVersion_MoveVersionCalledWhenGetVersionBeforeCreatedVersionReturnsVersion ()
    {
      var stubbedJiraService = MockRepository.Mock<IJiraProjectVersionService>();
      var stubbedJiraMovePositioner = MockRepository.Mock<IJiraVersionMovePositioner<SemanticVersion>>();
      var versionBeforeCreatedVersion = new JiraProjectVersionComparableAdapter<SemanticVersion>()
                                        {
                                          JiraProjectVersion = new JiraProjectVersion() { self = "irrelevantValue" },
                                          ComparableVersion = new SemanticVersion()
                                        };
      var stubbedJiraProjectVersion =
          new JiraProjectVersionComparableAdapter<SemanticVersion>() { JiraProjectVersion = new JiraProjectVersion() { id = "someID" } };


      stubbedJiraMovePositioner.Stub (x => x.HasToBeMoved()).Return (true);
      stubbedJiraMovePositioner.Stub (x => x.GetVersionBeforeCreatedVersion()).Return (versionBeforeCreatedVersion);
      stubbedJiraMovePositioner.Stub (x => x.GetCreatedVersion()).Return (stubbedJiraProjectVersion);

      var jiraSortVersion = new JiraSortVersion<SemanticVersion> (stubbedJiraService, stubbedJiraMovePositioner);

      jiraSortVersion.SortVersion();

      stubbedJiraService.AssertWasCalled (
          x => x.MoveVersion (
              Arg<string>.Is.Equal (stubbedJiraProjectVersion.JiraProjectVersion.id),
              Arg<string>.Is.Equal (versionBeforeCreatedVersion.JiraProjectVersion.self)));
      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersionByPosition (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
    }

    [Test]
    public void SortVersion_MoveVersionNotCalledWhenVersionBeforeCreatedVersionSemanticVersionEqualsCreatedVersion ()
    {
      var stubbedJiraService = MockRepository.Mock<IJiraProjectVersionService>();
      var stubbedJiraMovePositioner = MockRepository.Mock<IJiraVersionMovePositioner<SemanticVersion>>();
      var equalSemanticVersion = new SemanticVersion() { Major = 1, Minor = 0, Patch = 0 };
      var versionBeforeCreatedVersion = new JiraProjectVersionComparableAdapter<SemanticVersion>()
                                        {
                                          JiraProjectVersion = new JiraProjectVersion() { self = "irrelevantValue" },
                                          ComparableVersion = equalSemanticVersion
                                        };
      var stubbedJiraProjectVersion = new JiraProjectVersionComparableAdapter<SemanticVersion>()
                                      {
                                        JiraProjectVersion = new JiraProjectVersion() { id = "someID" },
                                        ComparableVersion = equalSemanticVersion
                                      };


      stubbedJiraMovePositioner.Stub (x => x.HasToBeMoved()).Return (true);
      stubbedJiraMovePositioner.Stub (x => x.GetVersionBeforeCreatedVersion()).Return (versionBeforeCreatedVersion);
      stubbedJiraMovePositioner.Stub (x => x.GetCreatedVersion()).Return (stubbedJiraProjectVersion);

      var jiraSortVersion = new JiraSortVersion<SemanticVersion> (stubbedJiraService, stubbedJiraMovePositioner);


      jiraSortVersion.SortVersion();

      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersion (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersionByPosition (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
    }
  }
}