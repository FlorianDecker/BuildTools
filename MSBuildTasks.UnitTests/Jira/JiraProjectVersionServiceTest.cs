﻿using System;
using System.Linq;
using System.Net;
using NUnit.Framework;
using Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacade;
using RestSharp;

namespace BuildTools.MSBuildTasks.UnitTests.Jira
{
  [TestFixture]
  public class JiraProjectVersionServiceTest
  {
    private const string c_jiraUrl = "http://s0316:8080/jira/rest/api/2/";
    private const string c_jiraUsername = "dominik.rauch";
    private const string c_jiraPassword = "Rubicon01";
    private const string c_jiraProjectKey = "RM";

    private JiraProjectVersionService _service;

    [SetUp]
    public void SetUp ()
    {
      _service = new JiraProjectVersionService (c_jiraUrl, c_jiraUsername, c_jiraPassword);
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
      var versions = _service.FindUnreleasedVersions (c_jiraProjectKey, "4.1.").ToList();
      Assert.That (versions.Count(), Is.EqualTo (3));

      var versionToRelease = versions.First();
      Assert.That (versionToRelease.name, Is.EqualTo ("4.1.0"));

      var versionToFollow = versions.Skip (1).First();
      Assert.That (versionToFollow.name, Is.EqualTo ("4.1.1"));

      var versions2 = _service.FindUnreleasedVersions (c_jiraProjectKey, "4.2.");
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
      versions = _service.FindUnreleasedVersions(c_jiraProjectKey, "4.1.").ToList();
      Assert.That (versions.Count(), Is.EqualTo (2));

      var versionThatFollowed = versions.First();
      Assert.That (versionThatFollowed.name, Is.EqualTo ("4.1.1"));

      // Check whether versionThatFollowed has all the non-closed issues from versionToRelease
      var issues = _service.FindAllNonClosedIssues (versionThatFollowed.id);
      Assert.That (issues.Count(), Is.EqualTo (2));
      
      // Check whether the additionalVersion still has its issue
      additionalVersion = _service.FindUnreleasedVersions (c_jiraProjectKey, "4.2.").First();
      var additionalVersionIssues = _service.FindAllNonClosedIssues (additionalVersion.id);
      Assert.That (additionalVersionIssues.Count(), Is.EqualTo (1));

      DeleteVersionsIfExistent (c_jiraProjectKey, "4.1.0", "4.1.1", "4.1.2", "4.2.0");
    }

    private void AddTestIssueToVersion (string summary, bool closed, params JiraProjectVersion[] toRelease)
    {
      // Create new issue
      var resource = "issue";
      var request = new RestRequest { Method = Method.POST, RequestFormat = DataFormat.Json, Resource = resource };

      var body = new { fields = new { project = new { key = c_jiraProjectKey }, issuetype = new { name = "Bug" }, summary = summary, fixVersions = toRelease.Select(v=>new{v.id}) } };
      request.AddBody (body);

      var response = _service.RestClient.Execute<JiraIssue> (request);
      if(response.StatusCode != HttpStatusCode.Created)
        throw new JiraException (string.Format("Error calling REST service, HTTP resonse is: {0}\nReturned content: {1}", response.StatusCode, response.Content));

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

      var response = _service.RestClient.Execute (request);
      if(response.StatusCode != HttpStatusCode.NoContent)
        throw new JiraException (string.Format("Error calling REST service, HTTP resonse is: {0}\nReturned content: {1}", response.StatusCode, response.Content));
    }

    [Test]
    public void TestGetUnreleasedVersionsWithNonExistentPattern ()
    {
      DeleteVersionsIfExistent (c_jiraProjectKey, "a.b.c.d");

      // Try to get an unreleased version with a non-existent pattern
      var versions = _service.FindUnreleasedVersions (c_jiraProjectKey, "a.b.c.d");
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