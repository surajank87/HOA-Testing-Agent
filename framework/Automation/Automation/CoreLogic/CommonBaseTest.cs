using CoreLogic.Managers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PolicyViewer.Pages;
using PolicyViewer;
using RestSharp;
using Utility.DataModels;
using Utility.Readers;
using Utility.Reporting;
using Utility;
using PolicyViewer.Driver;

namespace CoreLogic
{
    public class CommonBaseTest : ExtentReportBase
    {
        protected virtual string ConfigFilePath { get; } = @"Override this property in the Test Class and provide the JSON configuration file path";
        protected static Configuration Configuration { get; private set; }
        protected string PolicyViewerUrl { get; private set; }
        protected APIManager ApiManager { get; private set; }
        protected PolicyViewerPage PolicyViewerPage { get; private set; }
        protected SubmissionHandler SubmissionHandler { get; private set; }
        protected string CurrentQuotePolicyViewerUrl { get; private set; }
        protected string State { get; private set; }

        #region One-Time Setup
        [OneTimeSetUp]
        [Order(1)]
        public void OneTimeSetUp()
        {
            InitializeConfiguration();
            InitializeApiManager();
        }

        private void InitializeConfiguration()
        {
            Configuration = ConfigurationReader.ReadConfiguration(Helper.GetFullPath(ConfigFilePath));
            PolicyViewerUrl = PolicyViewerUrlManager.GetPolicyViewerUrl(Configuration.Environment);
        }

        private void InitializeApiManager()
        {
            string personalLineOrCommercialLine = GetPersonalOrCommercialLine();
            string envConfigurationFilePath = EnvironmentConfigurationManager.GetFilePath(Configuration.Environment);
            string baseUrl = JsonReader.GetValue(envConfigurationFilePath, "Baseurl");
            string apiKey = JsonReader.GetValue(envConfigurationFilePath, $"X-Api-Key.{personalLineOrCommercialLine}.{Configuration.Tenant}.{Configuration.SubTenant}");
            ApiManager = new APIManager(baseUrl, apiKey);
        }
        #endregion

        #region Setup
        [SetUp]
        [Order(1)]
        public void Setup()
        {
            DriverManager.GetDriver();
            DriverManager.GetDriver().Manage().Window.Maximize();
            DriverManager.GetDriver().Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(20);
            InitializePageObjects();
        }

        private void InitializePageObjects()
        {
            PolicyViewerPage = new PolicyViewerPage(DriverManager.GetDriver());
            SubmissionHandler = new SubmissionHandler(PolicyViewerPage);
        }
        #endregion

        #region TearDown
        [TearDown]
        [Order(1)]
        public void TearDown()
        {
            DriverManager.QuitDriver();
        }
        #endregion

        #region One-Time TearDown
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SaveReport(Configuration.Carrier, Configuration.LOB, Configuration.Environment, Configuration.State);
            FlushReport();
        }
        #endregion

        #region Submit and Capture Response
        public void SubmitAndCaptureResponse(string testCaseName, string state, string requestBody)
        {
            try
            {
                State = state;
                extentTest = extentReport.CreateTest(testCaseName);

                JObject requestBodyJson = JObject.Parse(requestBody);

                RestResponse response = ApiManager.SubmitQuoteAndGetResponse(requestBodyJson);
                string friendlyId = ApiManager.GetFriendlyId(response, extentTest);

                var submissionDetails = SubmissionHandler.GetSubmissionDetails(friendlyId, PolicyViewerUrl, Configuration);

                CurrentQuotePolicyViewerUrl = PolicyViewerPage.GetCurrentQuotePolicyViewerUrl();

                Reporter.LogSubmissionDetails(friendlyId, submissionDetails, Configuration, CurrentQuotePolicyViewerUrl);
                Console.WriteLine("Submission Status - " + submissionDetails["Status"]);

                SaveOutputFiles(testCaseName, requestBody, submissionDetails, friendlyId);
            }
            catch (AssertionException ex)
            {
                Reporter.LogException(ex, "Quote Submission Assertion Failed");
                throw;
            }
            catch (ArgumentException ex)
            {
                Reporter.LogException(ex, ex.Message);
                throw;
            }
        }

        private void SaveOutputFiles(string testCaseName, string requestBody,
            Dictionary<string, string> submissionDetails, string friendlyId)
        {
            string outputDir = Helper.GetFullPath(
                $@"TestProject\Output\{Configuration.Carrier}\{Configuration.State}");
            Directory.CreateDirectory(outputDir);

            File.WriteAllText(Path.Combine(outputDir, $"{testCaseName}_request.json"), requestBody);

            if (submissionDetails.ContainsKey("Request"))
                File.WriteAllText(Path.Combine(outputDir, $"{testCaseName}_carrier_request.txt"),
                    submissionDetails["Request"]);

            if (submissionDetails.ContainsKey("Response"))
                File.WriteAllText(Path.Combine(outputDir, $"{testCaseName}_carrier_response.txt"),
                    submissionDetails["Response"]);

            var details = new
            {
                FriendlyId = friendlyId,
                Status = submissionDetails["Status"],
                PolicyViewerUrl = CurrentQuotePolicyViewerUrl
            };
            File.WriteAllText(Path.Combine(outputDir, $"{testCaseName}_details.json"),
                JObject.FromObject(details).ToString());
        }

        private string GetPersonalOrCommercialLine()
        {
            return StringComparer.OrdinalIgnoreCase.Equals(Configuration.PersonalLineOrCommercialLine, "PersonalLine")
                ? "PersonalLine"
                : "CommercialLine";
        }
        #endregion
    }
}
