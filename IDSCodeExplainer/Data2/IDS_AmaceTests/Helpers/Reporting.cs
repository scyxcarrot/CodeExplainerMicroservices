using Rhino;
using System;
using System.Collections.Generic;

namespace IDS.Testing
{
    public static class Reporting
    {
        public static bool CompareValues(double expected, double actual, string description)
        {
            var matches = (Math.Abs(expected - actual) < 0.001);

            if (!matches)
            {
                RhinoApp.WriteLine("{0}: expected {1:F8}, actual {2:F8}", description, expected, actual);
            }

            return matches;
        }

        public static void ShowResultsInCommandLine(bool everythingSucceeded, string testTitle)
        {
            RhinoApp.WriteLine(everythingSucceeded ? "{0} Test Succeeded" : "{0} Test Failed", testTitle);
        }

        public static void ShowResultsInCommandLine(bool everythingSucceeded, string testTitle, List<string> logLines)
        {
            ShowResultsInCommandLine(everythingSucceeded, testTitle);

            if (everythingSucceeded) return;
            foreach (var line in logLines)
            {
                RhinoApp.WriteLine(line);
            }
        }
    }
}
