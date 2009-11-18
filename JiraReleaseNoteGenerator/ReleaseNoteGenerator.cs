// Copyright (c) 2009 rubicon informationstechnologie gmbh
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
using System;
using System.IO;
using System.Net;
using System.Xml.Linq;
using Remotion.BuildTools.JiraReleaseNoteGenerator.Utilities;

namespace Remotion.BuildTools.JiraReleaseNoteGenerator
{
  public class ReleaseNoteGenerator
  {
    private readonly Configuration _configuration;
    private JiraIssueAggregator _jiraIssueAggregator;
    
    public ReleaseNoteGenerator (Configuration configuration, JiraClient jiraClient)
    {
      ArgumentUtility.CheckNotNull ("configuration", configuration);

      _configuration = configuration;
      _jiraIssueAggregator = new JiraIssueAggregator (Configuration.Current, jiraClient);
    }

    public void GenerateReleaseNotes (string version)
    {
      ArgumentUtility.CheckNotNull ("version", version);

      var issues = _jiraIssueAggregator.GetXml (version);
      var config = XDocument.Load (Path.Combine("XmlUtilities", _configuration.ConfigFile));
      issues.Root.AddFirst (config.Elements());
      issues.Save ("JiraIssues.xml");

      var xmlTransformer = new XmlTransformer ("JiraIssues.xml", "ReleaseNotesForVersion" + version + ".html");
      xmlTransformer.GenerateHtmlFromXml();
    }
  }
}