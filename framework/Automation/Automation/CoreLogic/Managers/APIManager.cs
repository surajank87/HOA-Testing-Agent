using API_Collection.Constants;
using AventStack.ExtentReports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Utility.DataModels;
using Utility.Reporting;

namespace CoreLogic.Managers
{
    public class APIManager
    {
        private readonly IRestClient _client;
        private readonly string _API_Key;

        public APIManager(string baseurl, string API_Key)
        {
            string updateBaseurl = ProcessBaseUrl(baseurl);
            _client = new RestClient(updateBaseurl);
            _API_Key = API_Key;
        }

        private string ProcessBaseUrl(string baseurl)
        {
            if (baseurl.StartsWith("https://"))
            {
                return baseurl;
            }
            else
            {
                return "https://" + baseurl;
            }
        }

        private RestResponse CreateQuote(string jsonRequestBody)
        {
            RestRequest request = new RestRequest(APIContants.CREATE_QUOTE_URI, Method.Post);
            request.AddHeader("X-Api-Key", _API_Key);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", jsonRequestBody, ParameterType.RequestBody);
            return _client.ExecutePost(request);
        }

        private void SubmitQuote(string applicationId)
        {
            RestRequest request = new RestRequest(APIContants.SUBMIT_QUOTE_URI, Method.Post);
            request.AddHeader("X-Api-Key", _API_Key);
            request.AddHeader("Content-Type", "application/json");
            request.AddUrlSegment("applicationid", applicationId);
            RestResponse response = _client.ExecutePost(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), response.Content);
        }

        private RestResponse GetResults(string applicationId)
        {
            RestRequest request = new RestRequest(APIContants.GET_RESULT_URI, Method.Get);
            request.AddUrlSegment("applicationid", applicationId);
            request.AddHeader("X-Api-Key", _API_Key);
            return _client.ExecuteGet(request);
        }

        public string GetFriendlyId(RestResponse submissionResponse, ExtentTest node)
        {
            if (submissionResponse.Content == null)
            {
                throw new NullReferenceException("Unable to extract Friendly Id - Submisson Response content is null");
            }
            JObject submissionResponseInJson = JObject.Parse(submissionResponse.Content);
            JToken? friendlyId = submissionResponseInJson.SelectToken("friendlyId");
            if (friendlyId == null)
            {
                throw new Exception("friendlyId token is not present in response/null");
            }
            else
            {
                return friendlyId.ToString();
            }

        }

        // V2 Version
        public RestResponse SubmitQuoteAndGetResponse(JObject requestBody) //Always sending the data in JSON formate Why??
        {
            #region Create Quote, Submit Quote and Extract ApplicationId

            RestResponse response = CreateQuote(requestBody.ToString());
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), response.Content);
            JObject createQuoteResponseInJson = JObject.Parse(response.Content);
            string applicationId = createQuoteResponseInJson.SelectToken("id").ToString(); //Applicatin id is needed for submitting the Quote
            var validationErrors = createQuoteResponseInJson.SelectToken("validationErrors").ToString();
            List<ValidationDataModel> validationsList = JsonConvert.DeserializeObject<List<ValidationDataModel>>(validationErrors);
            int noOfValidations = validationsList.Count();
            if (noOfValidations > 0)
            {
                Exception ex = new Exception("Quote has below Validations\n" + validationErrors);
                Reporter.LogException(ex, "Quote has below Validations");
                throw ex;
            }
            SubmitQuote(applicationId);


            #endregion

            #region Get Results By Using ApplicationId

            RestResponse resultsResponse = GetResults(applicationId);
            Assert.That(resultsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var responseJson = JObject.Parse(resultsResponse.Content.ToString());
            string status = "";
            while (status != "Completed")
            {
                resultsResponse = GetResults(applicationId);
                responseJson = JObject.Parse(resultsResponse.Content.ToString()); //why second time this is required as we already taken response in JSON
                status = (string)responseJson["status"];
                Thread.Sleep(1000);
            }

            #endregion

            return response;
        }
    }

}
