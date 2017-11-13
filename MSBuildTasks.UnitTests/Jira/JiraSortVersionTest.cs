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
      var stubbedJiraMovePositioner = MockRepository.Mock<IJiraVersionMovePositioner>();

      stubbedJiraMovePositioner.Stub (x => x.HasToBeMoved()).Return (false);

      var jiraSortVersion = new JiraSortVersion (stubbedJiraService, stubbedJiraMovePositioner);

      var irrelevantJiraProjectVersion = new JiraProjectVersionSemVerAdapter();
      jiraSortVersion.SortVersion (irrelevantJiraProjectVersion);

      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersion (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersionByPosition (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
    }

    [Test]
    public void SortVersion_MoveVersionByPositionCalledWhenGetVersionBeforeCreatedVersionReturnsNull ()
    {
      var stubbedJiraService = MockRepository.Mock<IJiraProjectVersionService>();
      var stubbedJiraMovePositioner = MockRepository.Mock<IJiraVersionMovePositioner>();

      stubbedJiraMovePositioner.Stub (x => x.HasToBeMoved()).Return (true);
      stubbedJiraMovePositioner.Stub (x => x.GetVersionBeforeCreatedVersion()).Return (null);

      var jiraSortVersion = new JiraSortVersion (stubbedJiraService, stubbedJiraMovePositioner);

      var irrelevantJiraProjectVersion = new JiraProjectVersionSemVerAdapter () { JiraProjectVersion = new JiraProjectVersion () { id = "someID" } };
      jiraSortVersion.SortVersion (irrelevantJiraProjectVersion);

      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersion (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
      stubbedJiraService.AssertWasCalled (
          x => x.MoveVersionByPosition (Arg<string>.Is.Equal (irrelevantJiraProjectVersion.JiraProjectVersion.id), Arg<string>.Is.Equal ("First")));
    }

    [Test]
    public void SortVersion_MoveVersionCalledWhenGetVersionBeforeCreatedVersionReturnsVersion ()
    {
      var stubbedJiraService = MockRepository.Mock<IJiraProjectVersionService> ();
      var stubbedJiraMovePositioner = MockRepository.Mock<IJiraVersionMovePositioner> ();
      var versionBeforeCreatedVersion = new JiraProjectVersionSemVerAdapter()
                                        {
                                            JiraProjectVersion = new JiraProjectVersion() { self = "irrelevantValue" },
                                            SemanticVersion = new SemanticVersion()
                                        };

      stubbedJiraMovePositioner.Stub (x => x.HasToBeMoved()).Return (true);
      stubbedJiraMovePositioner.Stub (x => x.GetVersionBeforeCreatedVersion ()).Return (versionBeforeCreatedVersion);

      var jiraSortVersion = new JiraSortVersion (stubbedJiraService, stubbedJiraMovePositioner);

      var irrelevantJiraProjectVersion = new JiraProjectVersionSemVerAdapter() { JiraProjectVersion = new JiraProjectVersion() { id = "someID" } };
      jiraSortVersion.SortVersion (irrelevantJiraProjectVersion);

      stubbedJiraService.AssertWasCalled (x => x.MoveVersion (Arg<string>.Is.Equal (irrelevantJiraProjectVersion.JiraProjectVersion.id), Arg<string>.Is.Equal (versionBeforeCreatedVersion.JiraProjectVersion.self)));
      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersionByPosition (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
    }

    [Test]
    public void SortVersion_MoveVersionNotCalledWhenVersionBeforeCreatedVersionSemanticVersionEqualsCreatedVersion ()
    {
      var stubbedJiraService = MockRepository.Mock<IJiraProjectVersionService> ();
      var stubbedJiraMovePositioner = MockRepository.Mock<IJiraVersionMovePositioner> ();
      var equalSemanticVersion = new SemanticVersion() { Major = 1, Minor = 0, Patch = 0 };
      var versionBeforeCreatedVersion = new JiraProjectVersionSemVerAdapter()
                                        {
                                            JiraProjectVersion = new JiraProjectVersion() { self = "irrelevantValue" },
                                            SemanticVersion = equalSemanticVersion
                                        };

      stubbedJiraMovePositioner.Stub (x => x.HasToBeMoved()).Return (true);
      stubbedJiraMovePositioner.Stub (x => x.GetVersionBeforeCreatedVersion ()).Return (versionBeforeCreatedVersion);

      var jiraSortVersion = new JiraSortVersion (stubbedJiraService, stubbedJiraMovePositioner);

      var irrelevantJiraProjectVersion = new JiraProjectVersionSemVerAdapter()
                                         {
                                             JiraProjectVersion = new JiraProjectVersion() { id = "someID" },
                                             SemanticVersion = equalSemanticVersion
                                         };

      jiraSortVersion.SortVersion (irrelevantJiraProjectVersion);

      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersion (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
      stubbedJiraService.AssertWasNotCalled (x => x.MoveVersionByPosition (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
    }
  }
}
