using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Config;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utility.DataModels;
using Utility.Enum;
using Utility.Readers;
using Environment = Utility.Enum.Environment;

namespace Utility.Reporting
{
    public class ExtentReportBase
    {
        private static readonly object lockObj = new object();
        protected static ExtentReports extentReport;
        protected static ExtentSparkReporter htmlReporter;
        protected static ExtentTest extentTest;

        static ExtentReportBase()
        {
            InitializeExtentReports();
        }

        private static void InitializeExtentReports()
        {
            lock (lockObj)
            {
                if (extentReport == null)
                {
                    extentReport = new ExtentReports();
                }
            }
        }

        [TearDown]
        [Order(1)]
        public void TearDownTest()
        {
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            var stacktrace = string.IsNullOrEmpty(TestContext.CurrentContext.Result.StackTrace)
                ? ""
                : $"<pre>{TestContext.CurrentContext.Result.Message}</pre>";            
        }
        public void SaveReport(string carrier, string lob, Environment environment, string state)
        {
            lock (lockObj)
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string currentDirectory = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\Execution Reports"));
                string reportDirectory = currentDirectory;

                if (!Directory.Exists(reportDirectory))
                {
                    Directory.CreateDirectory(reportDirectory);
                }

                string dateTimeStamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm");
                string reportFilename = $"{state}/{state}_{carrier}_{lob}_{environment}_{dateTimeStamp}.html";
                string reportPath = Path.Combine(reportDirectory, reportFilename);

                htmlReporter = new ExtentSparkReporter(reportPath);
                htmlReporter.Config.TimelineEnabled = false;
                htmlReporter.Config.TimeStampFormat = "";
                htmlReporter.Config.ReportName = "Test Execution Report";
                htmlReporter.Config.Theme = Theme.Standard;

                extentReport.AttachReporter(htmlReporter);
            }
        }
        public void FlushReport()
        {
            lock (lockObj)
            {
                if (extentReport != null)
                {
                    extentReport.Flush();
                }
            }
        }
    }
}
