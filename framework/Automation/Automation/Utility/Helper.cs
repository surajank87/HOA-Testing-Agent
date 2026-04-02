using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public class Helper
    {
        public static string GetFullPath(string relativePath)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\.."));
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
            return fullPath;
        }
        public static string GetTestResult(string expectedMapping, string actualMapping)
        {
            return expectedMapping.Equals(actualMapping) ? "Passed" : "Failed";
        }
    }
}
