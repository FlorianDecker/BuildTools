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
using Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeInterfaces;
using System;

namespace Remotion.BuildTools.MSBuildTasks.Jira.Utility
{
  public class JiraSortVersion <T> where T : IComparable<T>
  {
    private readonly IJiraProjectVersionService _jiraService;
    private readonly IJiraVersionMovePositioner<T> _jiraVersionMovePositioner;

    public JiraSortVersion (IJiraProjectVersionService jiraService, IJiraVersionMovePositioner<T> jiraVersionMovePositioner)
    {
      _jiraService = jiraService;
      _jiraVersionMovePositioner = jiraVersionMovePositioner;
    }

    public void SortVersion ()
    {
      var createdVersion = _jiraVersionMovePositioner.GetCreatedVersion();
      if (_jiraVersionMovePositioner.HasToBeMoved ())
      {
        var versionBeforeCreatedVersion = _jiraVersionMovePositioner.GetVersionBeforeCreatedVersion ();
        if (versionBeforeCreatedVersion == null)
        {
          _jiraService.MoveVersionByPosition (createdVersion.JiraProjectVersion.id, "First");
        }
        else if (versionBeforeCreatedVersion.ComparableVersion == null || !versionBeforeCreatedVersion.ComparableVersion.Equals (createdVersion.ComparableVersion))
        {
          _jiraService.MoveVersion (createdVersion.JiraProjectVersion.id, versionBeforeCreatedVersion.JiraProjectVersion.self);
        }
      }
    }
  }
}
