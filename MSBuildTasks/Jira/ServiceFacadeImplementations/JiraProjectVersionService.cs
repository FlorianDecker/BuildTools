// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Remotion.BuildTools.MSBuildTasks.Jira.SemanticVersioning;
using Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeInterfaces;
using Remotion.BuildTools.MSBuildTasks.Jira.Utility;
using RestSharp;

namespace Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeImplementations
{
  public class JiraProjectVersionService : IJiraProjectVersionService
  {
    private readonly JiraRestClient jiraClient;
    private readonly JiraIssueService jiraIssueService;
    private readonly JiraProjectVersionFinder jiraProjectVersionFinder;

    public JiraProjectVersionService(JiraRestClient restClient)
    {
      jiraClient = restClient;
      jiraIssueService = new JiraIssueService (restClient);
      jiraProjectVersionFinder = new JiraProjectVersionFinder (restClient);
    }

    public string CreateVersion (string projectKey, string versionName, DateTime? releaseDate)
    {
      var request = jiraClient.CreateRestRequest ("version", Method.POST);

      if (releaseDate != null)
      {
          releaseDate = AdjustReleaseDateForJira(releaseDate.Value);
      }

      var projectVersion = new JiraProjectVersion { name = versionName, project = projectKey, releaseDate = releaseDate };
      request.AddBody(projectVersion);

      var newProjectVersion = jiraClient.DoRequest<JiraProjectVersion> (request, HttpStatusCode.Created);

      return newProjectVersion.Data.id;
    }

    public string CreateSubsequentVersion (string projectKey, string versionPattern, int versionComponentToIncrement, DayOfWeek versionReleaseWeekday)
    {
      // Determine next version name
      var lastUnreleasedVersion = jiraProjectVersionFinder.FindUnreleasedVersions (projectKey, versionPattern).Last ();
      var nextVersionName = IncrementVersion (lastUnreleasedVersion.name, versionComponentToIncrement);

      // Determine next release day
      if (!lastUnreleasedVersion.releaseDate.HasValue)
        throw new JiraException ("releaseDate of lastUnreleasedVersion must have a value but is null");

      var nextReleaseDay = lastUnreleasedVersion.releaseDate.Value.AddDays(1);
      while(nextReleaseDay.DayOfWeek != versionReleaseWeekday)
        nextReleaseDay = nextReleaseDay.AddDays (1);

      var newVersionId = CreateVersion (projectKey, nextVersionName, nextReleaseDay);
      MoveVersion (newVersionId, lastUnreleasedVersion.self);

      return newVersionId;
    }

    public void MoveVersion(string versionId, string afterVersionUrl)
    {
      var request = jiraClient.CreateRestRequest ("version/" + versionId + "/move", Method.POST);

      request.AddBody (new { after = afterVersionUrl });

      jiraClient.DoRequest (request, HttpStatusCode.OK);
    }

    public void MoveVersionByPosition (string versionId, string position)
    {
      var request = jiraClient.CreateRestRequest ("version/" + versionId + "/move", Method.POST);

      request.AddBody (new { position = position });

      jiraClient.DoRequest (request, HttpStatusCode.OK);
    }

    public JiraProjectVersion GetVersionById (string versionId)
    {
      var resource = "version/" + versionId;
      var request = jiraClient.CreateRestRequest (resource, Method.GET);

      var response = jiraClient.DoRequest<JiraProjectVersion> (request, HttpStatusCode.OK);
      return response.Data;
    }

    private string IncrementVersion (string version, int componentToIncrement)
    {
      if(componentToIncrement < 1 || componentToIncrement > 4)
        throw new ArgumentException ("componentToIncrement must be between 1 and 4");

      var versionParts = version.Split ('.').Select(int.Parse).ToArray();
      if(versionParts.Length < componentToIncrement)
        throw new ArgumentException(string.Format ("version must have at least {0} components", componentToIncrement));

      ++versionParts[componentToIncrement - 1];
      return versionParts.Select (p => p.ToString()).Aggregate ((l, r) => (l + "." + r));
    }

    public void ReleaseVersion (string versionID, string nextVersionID)
    {
      if(versionID != nextVersionID)
      {
        var nonClosedIssues = jiraIssueService.FindAllNonClosedIssues (versionID);
        jiraIssueService.MoveIssuesToVersion (nonClosedIssues, versionID, nextVersionID);
      }

      ReleaseVersion (versionID);
    }

    public void ReleaseVersionAndSquashUnreleased (string versionID, string nextVersionID, string projectKey)
    {
      if (versionID != nextVersionID)
      {
        var versions = jiraProjectVersionFinder.GetVersions (projectKey);
        
        SemanticVersionParser semVerParser = new SemanticVersionParser();
        var versionList = new JiraProjectVersionUtility().JiraProjectVersionsToJiraProjectVersionSemVerAdapters(semVerParser, versions);
        
        var orderedVersions = versionList.OrderBy(x => x.SemanticVersion).ToList();

        if (orderedVersions.Any(x => x.JiraProjectVersion.released != null && x.JiraProjectVersion.released == true))
        {
          var lastReleasedVersion =
            orderedVersions.Last(x => x.JiraProjectVersion.released != null && x.JiraProjectVersion.released == true);

          int indexOfLastReleasedVersion = orderedVersions.IndexOf(lastReleasedVersion);
          int indexOfCurrentVersion = orderedVersions.FindIndex(x => x.JiraProjectVersion.id == versionID);

          if ((indexOfLastReleasedVersion + 1) != indexOfCurrentVersion)
          {
            var unreleasedVersionsBeforeCurrentVersion = IndexRangeBetween(orderedVersions, indexOfLastReleasedVersion,
              indexOfCurrentVersion);

            foreach (var toBeSquashedVersion in unreleasedVersionsBeforeCurrentVersion)
            {
              string toBeSquashedVersionID = toBeSquashedVersion.JiraProjectVersion.id;

              var closedIssues = jiraIssueService.FindAllClosedIssues(toBeSquashedVersionID);
              jiraIssueService.MoveIssuesToVersion(closedIssues, toBeSquashedVersionID, versionID);

              var nonClosedIssues = jiraIssueService.FindAllNonClosedIssues(toBeSquashedVersionID);
              jiraIssueService.MoveIssuesToVersion(nonClosedIssues, toBeSquashedVersionID, nextVersionID);

              this.DeleteVersion(projectKey, toBeSquashedVersion.JiraProjectVersion.name);
            }
          }
        }

        ReleaseVersion(versionID, nextVersionID);
      }
    }

    private IEnumerable<TSource> IndexRangeBetween<TSource> (IList<TSource> source, int fromIndex, int toIndex)
    {
        int currentIndex = fromIndex + 1;
        
        while (currentIndex < toIndex)
        {
          yield return source[currentIndex];
          currentIndex++;
        }
    }

    private void ReleaseVersion(string versionID)
    {
      var resource = "version/" + versionID;
      var request = jiraClient.CreateRestRequest (resource, Method.PUT);

      var adjustedReleaseDate = AdjustReleaseDateForJira(DateTime.Today);
      var projectVersion = new JiraProjectVersion { id = versionID, released = true, releaseDate = adjustedReleaseDate };
      request.AddBody (projectVersion);

      jiraClient.DoRequest (request, HttpStatusCode.OK);
    }

    public void DeleteVersion (string projectKey, string versionName)
    {
      var versions = jiraProjectVersionFinder.GetVersions (projectKey);
      var versionToDelete = versions.SingleOrDefault (v => v.name == versionName);
      if(versionToDelete == null)
        throw new JiraException (string.Format("Error, version with name '{0}' does not exist in project '{1}'.", versionName, projectKey));

      var resource = "version/" + versionToDelete.id;
      var request = jiraClient.CreateRestRequest (resource, Method.DELETE);
      jiraClient.DoRequest (request, HttpStatusCode.NoContent);
    }

    private static DateTime AdjustReleaseDateForJira (DateTime releaseDate)
    {
      var releaseDateAsUtcTime = releaseDate.ToUniversalTime();
      var difference = releaseDate - releaseDateAsUtcTime;
      var adjustedReleaseDate = releaseDate + difference;
      return adjustedReleaseDate;
    }
  }
}