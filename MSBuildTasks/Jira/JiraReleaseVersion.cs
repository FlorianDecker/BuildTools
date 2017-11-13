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
using System.Linq;
using Microsoft.Build.Framework;
using Remotion.BuildTools.MSBuildTasks.Jira.SemanticVersioning;
using Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeImplementations;
using Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeInterfaces;
using Remotion.BuildTools.MSBuildTasks.Jira.Utility;

namespace Remotion.BuildTools.MSBuildTasks.Jira
{
  public class JiraReleaseVersion : JiraTask
  {
    [Required]
    public string VersionID { get; set; }

    [Required]
    public string NextVersionID { get; set; }

    private bool _sortReleasedVersion = false;

    public bool SortReleasedVersion
    {
      get { return _sortReleasedVersion; }
      set { _sortReleasedVersion = value; }
    }

    public override bool Execute ()
    {
      try
      {
        JiraRestClient restClient = new JiraRestClient (JiraUrl, Authenticator);
        IJiraProjectVersionService service = new JiraProjectVersionService (restClient);

        service.ReleaseVersion (VersionID, NextVersionID);

        if (SortReleasedVersion)
        {
          var semVerParser = new SemanticVersionParser();
          var finder = new JiraProjectVersionFinder(restClient);

          var toBeSortedJiraProjectVersion = service.GetVersionById (VersionID);
          var versions = finder.FindVersions (toBeSortedJiraProjectVersion.project, "(?s).*").ToList();
          var releasedVersions = versions.Where (x => x.released == true).ToList();

          var jiraVersionMovePositioner = new JiraVersionMovePositioner (releasedVersions, toBeSortedJiraProjectVersion);
          var jiraSortVersion = new JiraSortVersion (service, jiraVersionMovePositioner);

          jiraSortVersion.SortVersion (
              new JiraProjectVersionSemVerAdapter()
              {
                JiraProjectVersion = toBeSortedJiraProjectVersion,
                SemanticVersion = semVerParser.ParseVersion (toBeSortedJiraProjectVersion.name)
              });
        }

        return true;
      }
      catch(Exception ex)
      {
        Log.LogErrorFromException (ex);
        return false;
      }
    }
  }
}