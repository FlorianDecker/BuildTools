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
using Remotion.BuildTools.MSBuildTasks.Jira.SemanticVersioning;
using Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeImplementations;

namespace Remotion.BuildTools.MSBuildTasks.Jira.Utility
{
  public class JiraVersionMovePositioner : IJiraVersionMovePositioner
  {
    private readonly List<JiraProjectVersion> _jiraProjectVersions;
    private readonly IOrderedEnumerable<JiraProjectVersionSemVerAdapter> _orderedVersions;
    private readonly SemanticVersion _createdVersionAsSemanticVersion;
    private readonly List<JiraProjectVersionSemVerAdapter> _toBeOrdered;

    public JiraVersionMovePositioner (List<JiraProjectVersion> jiraProjectVersions, JiraProjectVersion createdVersion)
    {
      _jiraProjectVersions = jiraProjectVersions;

      var semVerParser = new SemanticVersionParser();
      _createdVersionAsSemanticVersion = semVerParser.ParseVersion (createdVersion.name);

      _toBeOrdered = _jiraProjectVersions.Select (
        x => new JiraProjectVersionSemVerAdapter()
             {
               JiraProjectVersion = x,
               SemanticVersion = semVerParser.ParseVersionOrNull (x.name)
             }).ToList();

      _toBeOrdered.Add (
          new JiraProjectVersionSemVerAdapter()
          {
            JiraProjectVersion = createdVersion,
            SemanticVersion = _createdVersionAsSemanticVersion
          });

      _orderedVersions = _toBeOrdered.OrderBy (x => x.SemanticVersion);
    }

    public bool HasToBeMoved ()
    {
      if (_jiraProjectVersions.Count == 0)
        return false;

      if (Equals (PrivateGetVersionBeforeCreatedVersion(), GetVersionBeforeCreatedVersion()))
        return false;

      return true;
    }

    public JiraProjectVersionSemVerAdapter GetVersionBeforeCreatedVersion ()
    {
      return _orderedVersions.TakeWhile (x => !Equals (x.SemanticVersion, _createdVersionAsSemanticVersion)).LastOrDefault();
    }

    private JiraProjectVersionSemVerAdapter PrivateGetVersionBeforeCreatedVersion ()
    {
      return _toBeOrdered.TakeWhile (x => !Equals (x.SemanticVersion, _createdVersionAsSemanticVersion)).LastOrDefault();
    }
  }
}
