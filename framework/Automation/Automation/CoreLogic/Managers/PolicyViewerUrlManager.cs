using PolicyViewer;
using System;
using Utility.Enum;
using Environment = Utility.Enum.Environment;

namespace CoreLogic.Managers
{
    public class PolicyViewerUrlManager
    {
        private static readonly string QA = $"https://{Credentials.USERNAME}:{Credentials.PASSWORD}@support-qa.boltqa.com/PolicyViewer/SearchPolicy";
        private static readonly string UAT = $"https://{Credentials.USERNAME}:{Credentials.PASSWORD}@support-uat.progressivebolt.com/PolicyViewer/ViewPolicy";

        public static string GetPolicyViewerUrl(Environment environment)
        {
            switch (environment)
            {
                case Environment.QA:
                    // return GetSecureUrl(QA_PolicyViewerUrl); // Code to read credentials from Environment Variables using Debbuger tool
                    return QA;
                case Environment.UAT:
                    return UAT;
                default:
                    throw new ArgumentException($"Policy Viewer Url is not available for {environment} Environment");
            }
        }
        /*private static string GetSecureUrl(string baseUrl)
        {
            string? username = Environment.GetEnvironmentVariable("Username");
            string? password = Environment.GetEnvironmentVariable("Password");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new Exception("Username or password environment variable is not set.");
            }
            string secureUrl = $"https://{username}:{password}@{baseUrl}";
            return secureUrl;
        }*/
    }
}
