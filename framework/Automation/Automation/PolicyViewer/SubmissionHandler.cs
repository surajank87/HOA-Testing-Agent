using PolicyViewer.Pages;
using Utility.DataModels;

namespace PolicyViewer
{
    public class SubmissionHandler
    {
        private PolicyViewerPage _policyViewer;
        public SubmissionHandler(PolicyViewerPage policyViewer)
        {
            _policyViewer = policyViewer;
        }
        
        public Dictionary<string, string> GetSubmissionDetails(string friendlyId, string policyViewerUrl, Configuration configuration)
        {
            _policyViewer.NavigateToPolicyViewerAndShowStageDetails(policyViewerUrl, configuration.Tenant, friendlyId);
            string status = _policyViewer.GetSubmissionStatus(configuration.Carrier, configuration.LOB);
            Dictionary<string, string> submissionDetails = new Dictionary<string, string>();
            switch (status)
            {
                case "Success":
                    submissionDetails["Status"] = status;
                    submissionDetails["Request"] = _policyViewer.GetText(configuration.Carrier, configuration.LOB, "Request");
                    return submissionDetails;
                case "Declined":
                    submissionDetails["Status"] = status;
                    submissionDetails["Request"] = _policyViewer.GetText(configuration.Carrier, configuration.LOB, "Request");
                    submissionDetails["Response"] = _policyViewer.GetText(configuration.Carrier, configuration.LOB, "Response");
                    submissionDetails["ResultMessages"] = _policyViewer.GetText(configuration.Carrier, configuration.LOB, "ResultMessages");
                    return submissionDetails;
                case "Failed":
                    submissionDetails["Status"] = status;
                    submissionDetails["Request"] = _policyViewer.GetText(configuration.Carrier, configuration.LOB, "Request");
                    submissionDetails["Response"] = _policyViewer.GetText(configuration.Carrier, configuration.LOB, "Response");
                    //submissionDetails["ResultMessages"] = _policyViewer.GetText(configuration.Carrier, configuration.LOB, "ResultMessages");
                    return submissionDetails;                
                case "SubmissionReferral":
                    submissionDetails["Status"] = status;
                    submissionDetails["ReferalList"] = _policyViewer.GetText(configuration.Carrier, configuration.LOB, "ReferalList");
                    return submissionDetails;
                case "TechnicalError":
                    submissionDetails["Status"] = status;
                    submissionDetails["ResultMessages"] = _policyViewer.GetText(configuration.Carrier, configuration.LOB, "ResultMessages");
                    return submissionDetails;
                default:
                    throw new Exception("Status is other than Success/Failed/Declined/SubmissionReferral");
            }
        }
    }
}
