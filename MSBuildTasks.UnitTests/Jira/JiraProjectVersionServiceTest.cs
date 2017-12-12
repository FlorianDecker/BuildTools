using System;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using Remotion.BuildTools.MSBuildTasks.Jira;
using Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeImplementations;
using Remotion.BuildTools.MSBuildTasks;
using RestSharp;
using JsonSerializer = RestSharp.Serializers.JsonSerializer;

namespace BuildTools.MSBuildTasks.UnitTests.Jira
{
  [TestFixture]
  public class JiraProjectVersionServiceTest
  {
    private const string c_jiraUrl = "https://www.re-motion.org/jira/rest/api/2/";
    private const string c_jiraProjectKey = "SRCBLDTEST";
    private const string c_jiraUsername = "floriandeckerrubicon";
    private const string c_jiraPassword = "rubicon01";

    private JiraProjectVersionService _service;
    private JiraProjectVersionFinder _versionFinder;
    private JiraIssueService _issueService;
    private JiraRestClient _restClient;

    [SetUp]
    public void SetUp ()
    {
      
      IAuthenticator authenticator = new HttpBasicAuthenticator(c_jiraUsername, c_jiraPassword);
      _restClient = new JiraRestClient (c_jiraUrl, authenticator);
      _service = new JiraProjectVersionService (_restClient);
      _versionFinder = new JiraProjectVersionFinder (_restClient);
      _issueService = new JiraIssueService (_restClient);
    }

    [Test]
    public void Call_JiraCheckAuthentication()
    {
      var jiraCheckAuthenticationTask = new JiraCheckAuthentication
      {
        JiraProject = c_jiraProjectKey,
        JiraUrl = c_jiraUrl,
        JiraUsername = c_jiraUsername,
        JiraPassword = c_jiraPassword

      };
      jiraCheckAuthenticationTask.Execute();
    }

    [Test]
    public void IntegrationTest ()
    {
      DeleteVersionsIfExistent (c_jiraProjectKey, "4.1.0", "4.1.1", "4.1.2", "4.2.0");

      // Create versions
      _service.CreateVersion (c_jiraProjectKey, "4.1.0", DateTime.Today.AddDays(1));
      _service.CreateSubsequentVersion(c_jiraProjectKey, "4\\.1\\..*", 3, DayOfWeek.Monday);
      _service.CreateSubsequentVersion(c_jiraProjectKey, "4\\.1\\..*", 3, DayOfWeek.Tuesday);
      _service.CreateVersion (c_jiraProjectKey, "4.2.0", DateTime.Today.AddDays(7));

      // Get latest unreleased version
      var versions = _versionFinder.FindUnreleasedVersions (c_jiraProjectKey, "4.1.").ToList();
      Assert.That (versions.Count(), Is.EqualTo (3));

      var versionToRelease = versions.First();
      Assert.That (versionToRelease.name, Is.EqualTo ("4.1.0"));

      var versionToFollow = versions.Skip (1).First();
      Assert.That (versionToFollow.name, Is.EqualTo ("4.1.1"));

      var versions2 = _versionFinder.FindUnreleasedVersions (c_jiraProjectKey, "4.2.");
      Assert.That (versions2.Count(), Is.EqualTo (1));

      var additionalVersion = versions2.First();
      Assert.That (additionalVersion.name, Is.EqualTo ("4.2.0"));

      // Add issues to versionToRelease
      AddTestIssueToVersion ("My Test", false, versionToRelease);      
      AddTestIssueToVersion ("My closed Test", true, versionToRelease);
      AddTestIssueToVersion ("My multiple fixVersion Test", false, versionToRelease, additionalVersion);

      // Release version
      _service.ReleaseVersion (versionToRelease.id, versionToFollow.id);

      // Get latest unreleased version again
      versions = _versionFinder.FindUnreleasedVersions (c_jiraProjectKey, "4.1.").ToList ();
      Assert.That (versions.Count(), Is.EqualTo (2));

      var versionThatFollowed = versions.First();
      Assert.That (versionThatFollowed.name, Is.EqualTo ("4.1.1"));

      // Check whether versionThatFollowed has all the non-closed issues from versionToRelease
      var issues = _issueService.FindAllNonClosedIssues (versionThatFollowed.id);
      Assert.That (issues.Count(), Is.EqualTo (2));
      
      // Check whether the additionalVersion still has its issue
      additionalVersion = _versionFinder.FindUnreleasedVersions (c_jiraProjectKey, "4.2.").First();
      var additionalVersionIssues = _issueService.FindAllNonClosedIssues (additionalVersion.id);
      Assert.That (additionalVersionIssues.Count(), Is.EqualTo (1));

      DeleteVersionsIfExistent (c_jiraProjectKey, "4.1.0", "4.1.1", "4.1.2", "4.2.0");
    }

    private void AddTestIssueToVersion (string summaryOfIssue, bool closed, params JiraProjectVersion[] toRelease)
    {
      // Create new issue
      var resource = "issue";
      var request = new RestRequest { Method = Method.POST, RequestFormat = DataFormat.Json, Resource = resource};

      var body = new { fields = new { project = new { key = c_jiraProjectKey }, issuetype = new { name = "Task" }, summary = summaryOfIssue, description = "testDescription", fixVersions = toRelease.Select(v=>new{v.id}) } };
      request.AddBody (body);
      
      var response = _restClient.DoRequest<JiraIssue> (request, HttpStatusCode.Created);

      // Close issue if necessary
      if(closed)
      {
        var issue = response.Data;
        CloseIssue (issue.id);
      }
    }

    private void CloseIssue(string issueID)
    {
      var resource = "issue/" + issueID + "/transitions";
      var request = new RestRequest { Method = Method.POST, RequestFormat = DataFormat.Json, Resource = resource };

      var body = new { transition = new { id = 2} };
      request.AddBody (body);

      _restClient.DoRequest(request, HttpStatusCode.NoContent);
    }

    [Test]
    public void TestGetUnreleasedVersionsWithNonExistentPattern ()
    {
      DeleteVersionsIfExistent (c_jiraProjectKey, "a.b.c.d");

      // Try to get an unreleased version with a non-existent pattern
      var versions = _versionFinder.FindUnreleasedVersions (c_jiraProjectKey, "a.b.c.d");
      Assert.That (versions.Count(), Is.EqualTo (0));
    }

    [Test]
    public void TestCannotCreateVersionTwice ()
    {
      DeleteVersionsIfExistent (c_jiraProjectKey, "5.0.0");

      // Create version
      _service.CreateVersion (c_jiraProjectKey, "5.0.0", DateTime.Today.AddDays(14));

      // Try to create same version again, should throw
      Assert.Throws (typeof (JiraException), () => _service.CreateVersion (c_jiraProjectKey, "5.0.0", DateTime.Today.AddDays(14+1)));

      DeleteVersionsIfExistent (c_jiraProjectKey, "5.0.0");
    }

    [Test]
    public void TestDeleteVersion ()
    {
      DeleteVersionsIfExistent (c_jiraProjectKey, "6.0.0.0");

      _service.CreateVersion (c_jiraProjectKey, "6.0.0.0", DateTime.Today.AddDays(21));
      _service.DeleteVersion (c_jiraProjectKey, "6.0.0.0");
    }

    [Test]
    public void TestDeleteNonExistentVersion ()
    {
      DeleteVersionsIfExistent (c_jiraProjectKey, "6.0.0.0");

      Assert.Throws (typeof (JiraException), () => _service.DeleteVersion (c_jiraProjectKey, "6.0.0.0"));
    }

    [Test]
    public void TestReleaseVersionAndSquashUnreleased ()
    {
      DeleteVersionsIfExistent (c_jiraProjectKey, "6.0.1-alpha.1", "6.0.1-alpha.2", "6.0.1-beta.1", "6.0.1-beta.2");
    
      _service.CreateVersion (c_jiraProjectKey, "6.0.1-alpha.1", null);
      _service.CreateVersion (c_jiraProjectKey, "6.0.1-alpha.2", null);
      _service.CreateVersion (c_jiraProjectKey, "6.0.1-beta.1", null);
      _service.CreateVersion (c_jiraProjectKey, "6.0.1-beta.2", null);

      var alpha1Version = _versionFinder.FindVersions(c_jiraProjectKey, "6.0.1-alpha.1").First();
      var alpha2Version = _versionFinder.FindVersions(c_jiraProjectKey, "6.0.1.alpha.2").First();
      var beta1Version = _versionFinder.FindVersions(c_jiraProjectKey, "6.0.1-beta.1").First();
      var beta2Version = _versionFinder.FindVersions(c_jiraProjectKey, "6.0.1-beta.2").First();

      AddTestIssueToVersion ("ClosedIssue", true, alpha1Version);
      AddTestIssueToVersion ("ClosedIssue", true, alpha2Version);
      AddTestIssueToVersion ("ClosedIssue", true, beta1Version);
      AddTestIssueToVersion ("ClosedIssue", true, beta2Version);

      AddTestIssueToVersion ("Open issues", false, alpha1Version);
      AddTestIssueToVersion ("Open issues", false, alpha2Version);
      AddTestIssueToVersion ("Open issues", false, beta1Version);
      AddTestIssueToVersion ("Open issues", false, beta2Version);

      _service.ReleaseVersion(alpha1Version.id, alpha2Version.id);

      _service.ReleaseVersionAndSquashUnreleased(beta1Version.id, beta2Version.id, c_jiraProjectKey);

      Assert.That (_versionFinder.FindVersions(c_jiraProjectKey, "6.0.1-alpha.2").Count(), Is.EqualTo(0));
      
      //Assert that the Closed Issue of deleted alpha2Version got moved to beta1Version
      Assert.That (_issueService.FindAllClosedIssues(beta1Version.id).Count(), Is.EqualTo(2));

      //Assert that the Open Issues of deleted alpha2Version and released beta1Version got moved to beta2Version
      Assert.That (_issueService.FindAllNonClosedIssues(beta2Version.id).Count(), Is.EqualTo(3));

      DeleteVersionsIfExistent (c_jiraProjectKey, "6.0.1-alpha.1", "6.0.1-alpha.2", "6.0.1-beta.1", "6.0.1-beta.2");
    }

    [Test]
    public void TestSortingNetVersion ()
    {
      const string firstVersion = "1.16.32.0";
      const string secondVersion = "1.16.32.1";
      const string thirdVersion = "1.16.32.2";

      DeleteVersionsIfExistent (c_jiraProjectKey, firstVersion, secondVersion, thirdVersion);

      _service.CreateVersion (c_jiraProjectKey, firstVersion, null);
      _service.CreateVersion (c_jiraProjectKey, thirdVersion, null);
      var toBeRepairedVersionId = _service.CreateVersion (c_jiraProjectKey, secondVersion, null);
      _repairer.RepairVersionPosition (toBeRepairedVersionId);

      var versions = _versionFinder.FindVersions (c_jiraProjectKey, "(?s).*").ToList();

      var positionFirstVersion = versions.IndexOf (versions.Single (x => x.name == firstVersion));
      var positionSecondVersion = versions.IndexOf (versions.Single (x => x.name == secondVersion));
      var positionThirdVersion = versions.IndexOf (versions.Single (x => x.name == thirdVersion));


      Assert.That (positionFirstVersion < positionSecondVersion, Is.True);
      Assert.That (positionSecondVersion < positionThirdVersion, Is.True);
    }

    [Test]
    public void TestSortingSemanticVersion ()
    {
      const string firstVersion = "2.1.3";
      const string secondVersion = "2.2.0-alpha.5";
      const string thirdVersion = "2.2.0";

      DeleteVersionsIfExistent (c_jiraProjectKey, firstVersion, secondVersion, thirdVersion);

      _service.CreateVersion (c_jiraProjectKey, firstVersion, null);
      _service.CreateVersion (c_jiraProjectKey, thirdVersion, null);
      var toBeRepairedVersionId = _service.CreateVersion (c_jiraProjectKey, secondVersion, null);
      _repairer.RepairVersionPosition (toBeRepairedVersionId);

      var versions = _versionFinder.FindVersions (c_jiraProjectKey, "(?s).*").ToList();

      var positionFirstVersion = versions.IndexOf (versions.Single (x => x.name == firstVersion));
      var positionSecondVersion = versions.IndexOf (versions.Single (x => x.name == secondVersion));
      var positionThirdVersion = versions.IndexOf (versions.Single (x => x.name == thirdVersion));


      Assert.That (positionFirstVersion < positionSecondVersion, Is.True);
      Assert.That (positionSecondVersion < positionThirdVersion, Is.True);
    }

    [Test]
    public void TestSortingWithInvalidVersions ()
    {
      const string firstVersion = "1.17.21.0";
      const string secondVersion = "NotValidVersion";
      const string thirdVersion = "1.16.31.0";
      const string betweenFirstAndSecondVersion = "1.17.22.0";

      DeleteVersionsIfExistent (c_jiraProjectKey, firstVersion, secondVersion, thirdVersion, betweenFirstAndSecondVersion);

      _service.CreateVersion (c_jiraProjectKey, firstVersion, null);
      _service.CreateVersion (c_jiraProjectKey, secondVersion, null);
      _service.CreateVersion (c_jiraProjectKey, thirdVersion, null);
      var toBeRepairedVersionId = _service.CreateVersion (c_jiraProjectKey, betweenFirstAndSecondVersion, null);
      _repairer.RepairVersionPosition (toBeRepairedVersionId);

      var versions = _versionFinder.FindVersions (c_jiraProjectKey, "(?s).*").ToList();

      var positionFirstVersion = versions.IndexOf (versions.Single (x => x.name == firstVersion));
      var positionbetweenFirstAndSecondVersion = versions.IndexOf (versions.Single (x => x.name == betweenFirstAndSecondVersion));
      var positionSecondVersion = versions.IndexOf (versions.Single (x => x.name == secondVersion));
      var positionThirdVersion = versions.IndexOf (versions.Single (x => x.name == thirdVersion));


      Assert.That (positionFirstVersion < positionbetweenFirstAndSecondVersion, Is.True);
      Assert.That (positionbetweenFirstAndSecondVersion < positionSecondVersion, Is.True);
      Assert.That (positionSecondVersion < positionThirdVersion, Is.True);
    }

    private void DeleteVersionsIfExistent (string projectName, params string[] versionNames)
    {
      foreach (var versionName in versionNames)
      {
        try
        {
          _service.DeleteVersion (projectName, versionName);
        }
        catch
        {
          // ignore
        }
      }
    }
  }
}