using CoreLogic;
using Utility;
using Utility.Readers;

namespace TestProject.Tests
{
    [TestFixture]
    public class Test : CommonBaseTest
    {
        protected override string ConfigFilePath { get; } = @"TestProject\config.json";

        public static IEnumerable<TestCaseData> GetTestData()
        {
            var config = ConfigurationReader.ReadConfiguration(Helper.GetFullPath(@"TestProject\config.json"));
            string requestBodiesPath = Helper.GetFullPath(
                $@"TestProject\RequestBodies\{config.Carrier}\{config.State}");
            var requestBodies = RequestBodyReader.ReadRequestBodies(requestBodiesPath);

            foreach (var entry in requestBodies)
            {
                yield return new TestCaseData(entry.Key, config.State, entry.Value)
                    .SetName($"TestCase: {entry.Key}");
            }
        }

        [TestCaseSource(nameof(GetTestData))]
        public void SubmitQuote(string testCaseName, string state, string requestBody)
        {
            SubmitAndCaptureResponse(testCaseName, state, requestBody);
        }
    }
}
