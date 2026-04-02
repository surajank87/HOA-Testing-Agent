using AventStack.ExtentReports;
using AventStack.ExtentReports.MarkupUtils;
using Utility.DataModels;

namespace Utility.Reporting
{
    public class Reporter : ExtentReportBase
    {
        public static void LogSubmissionDetails(string friendlyId, Dictionary<string, string> submissionDetails, Configuration configuration, string currentQuotePolicyViewerUrl)
        {
            string[,] submissionDetailsTable = new string[,]
            {
                { "Environment", configuration.Environment.ToString() },
                { "Tenant", configuration.Tenant.ToString() },
                { "Sub-Tenant", configuration.SubTenant.ToString() },
                { "Carrier", configuration.Carrier.ToString() },
                { "LOB", configuration.LOB.ToString() },
                { "State", configuration.State },
                { "Friendly Id", friendlyId },
                { "Submission Status", submissionDetails["Status"] },
                { "Policy Viewer Url", $"<a href={currentQuotePolicyViewerUrl} target=\"_blank\">Policy Viewer Url</a>" }
            };
            var node = extentTest.CreateNode("Quote Submission Details");
            switch (submissionDetails["Status"])
            {
                case "SubmissionReferral":
                    node.Fail(MarkupHelper.CreateTable(submissionDetailsTable));
                    node.Info("Referral List");
                    node.Info(MarkupHelper.CreateCodeBlock(submissionDetails["ReferalList"]));
                    throw new Exception($"{submissionDetails["Status"]}\n{submissionDetails["ReferalList"]}");

                case "TechnicalError":
                    node.Fail(MarkupHelper.CreateTable(submissionDetailsTable));
                    node.Info("Result Message");
                    node.Info(MarkupHelper.CreateCodeBlock(submissionDetails["ResultMessages"]));
                    throw new Exception($"{submissionDetails["Status"]}\n{submissionDetails["ResultMessages"]}");
                case "Failed":
                    node.Warning(MarkupHelper.CreateTable(submissionDetailsTable));
                    node.Info("Response");
                    node.Info(MarkupHelper.CreateCodeBlock(submissionDetails["Response"]));
                    break;
                case "Success":
                case "Declined":
                    node.Pass(MarkupHelper.CreateTable(submissionDetailsTable));
                    break;
            }
        }

        public static void LogException(Exception ex, string nodeHeading)
        {
            extentTest.CreateNode(nodeHeading).Fail(MarkupHelper.CreateCodeBlock(ex.Message));
        }
    }
}
