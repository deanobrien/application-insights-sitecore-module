using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;
using System.Collections.Generic;

namespace DeanOBrien.Feature.ApplicationInsights.Models
{
    public class ApplicationDetails
    {
        public Application Application { get; set; }
        public List<Application> FirstTier { get; set; }
        public List<Application> SecondTier { get; set; }
        public List<Application> ThirdTier { get; set; }
        public List<Application> ForthTier { get; set; }
        public List<Application> FifthTier { get; set; }
        public string TimeSpan { get; set; }
        public string ProblemIdBase64 { get; set; }
        public string InnerMessageBase64 { get; set; }
        public SummaryOfExceptions SummaryOfExceptions { get; set; }
        public string ErrorMessage { get; set; }
    }
}