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

using System.Net;
using RestSharp;

namespace Remotion.BuildTools.MSBuildTasks.Jira.ServiceFacadeImplementations
{
  public class JiraRestClient
  {
    private readonly RestClient _client;

    public RestClient RestClient
    {
      get { return _client; }
    }

    public JiraRestClient(string jiraUrl, IAuthenticator authenticator)
    {
      _client = new RestClient (jiraUrl) { Authenticator = authenticator };
    }

    public IRestRequest CreateRestRequest (string resource, Method method)
    {
      var request = new RestRequest () { Method = method, RequestFormat = DataFormat.Json, Resource = resource };
      return request;
    }

    public void DoRequest (IRestRequest request, HttpStatusCode successCode)
    {
      DoRequest<object> (request, successCode);
    }

    public IRestResponse<T> DoRequest<T> (IRestRequest request, HttpStatusCode successCode) where T : new ()
    {
      var response = _client.Execute<T> (request);
      if (response.StatusCode != successCode)
        throw new JiraException (string.Format ("Error calling REST service, HTTP response is: {0}\nReturned content: {1}", response.StatusCode, response.Content)) { HttpStatusCode = response.StatusCode };

      return response;
    }
  }
}
