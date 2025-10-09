using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;
using Sitecore.Pipelines.Loader;
using System.Collections.Generic;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights
{
    public interface IAppInsightsApi
    {
        void Initialize(string entraClientID, string entraClientSecret, string tenantId);
        List<GroupedException> GetGroupedExceptions(string applicationInsightsId, string applicationInsightsKey, string timespan);
        List<GroupedException> GetGroupedExceptions(string applicationInsightsId, string applicationInsightsKey, string problemIdBase64, string innerMostMessageBase64, string timespan);
        List<GroupedException> GetGroupedExceptionsV2(string applicationInsightsId, string timespan);
        List<GroupedException> GetGroupedExceptionsV2(string applicationInsightsId, string problemIdBase64, string innerMostMessageBase64, string timespan);
        List<CustomEventSummary> GetCustomEventsV2(string applicationInsightsId, string customEvent, string timespan);
        List<SingleException> GetSingleException(string applicationInsightsId, string problemIdBase64, string innerMostMessageBase64, string timespan);

    }
}
