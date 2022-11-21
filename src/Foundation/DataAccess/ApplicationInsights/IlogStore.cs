using DeanOBrien.Foundation.DataAccess.Models;
using System;
using System.Collections.Generic;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights
{
    public interface ILogStore
    {
        Application GetApplication(string applicationId);
        Application AddApplication(string applicationId, string title, string applicationInsightsId, string applicationInsightsKey);
        void SetNextHourly(string applicationId, DateTime nextDaily);
        void SetNextDaily(string applicationId, DateTime nextDaily);
        void AddGroupedException(string applicationId, GroupedException exception, AppInsightType type);
        void AddGroupedException(string applicationId, AppInsightType applicationType, string problemId, string problemIdBase64, string outerType, string type, string innermostType, string outerAssembly, string assembly, string outerMethod, string method, int count);
        List<GroupedException> GetGroupedExceptions(string applicationId, AppInsightType appInsightType, string timespan);
        List<GroupedException> GetGroupedExceptions(string applicationId, string problemIdBase64, AppInsightType appInsightType, string timespan);
    }
}
