using DeanOBrien.Foundation.DataAccess.Models;
using System.Collections.Generic;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights
{
    public interface IAppInsightsApi
    {
        List<GroupedException> GetGroupedExceptions(string applicationInsightsId, string applicationInsightsKey, string timespan);
        List<GroupedException> GetGroupedExceptions(string applicationInsightsId, string applicationInsightsKey, string problemIdBase64, string innerMostMessageBase64, string timespan);
    }
}
