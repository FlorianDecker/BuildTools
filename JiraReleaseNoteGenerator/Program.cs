﻿// This file is part of the re-motion Build Tools (www.re-motion.org)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Build Tools are free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// version 3.0 as published by the Free Software Foundation.
// 
// re-motion is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-motion; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.IO;
using System.Net;
using Remotion.BuildTools.JiraReleaseNoteGenerator.Utility;
using System.Linq;

namespace Remotion.BuildTools.JiraReleaseNoteGenerator
{
  public class Program
  {
    private static int Main (string[] args)
    {
      var argumentCheckResult = CheckArguments (args);
      if (argumentCheckResult != 0)
        return (argumentCheckResult);

      Console.Out.WriteLine ("Starting Remotion.BuildTools for version " + args[0]);

      Console.In.ReadLine();

      return 0;
    }


    public static int CheckArguments ( string[] arguments)
    {
      ArgumentUtility.CheckNotNull ("arguments", arguments);
      
      if (arguments.Length != 1)
      {
        Console.Out.WriteLine ("usage: Remotion.BuildTools versionNumber");
        return 1;
      }

      return 0;
    }

  }
}