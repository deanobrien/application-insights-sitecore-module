using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights;
using DeanOBrien.Foundation.DataAccess.Models;

namespace DeanOBrien.Feature.ApplicationInsights.Tasks
{
    public class AppInsightsImport
    {
        private ILogStore _logStore;
        private IAppInsightsApi _appInsightsApi;

        public AppInsightsImport()
        {
            _logStore = ServiceLocator.ServiceProvider.GetService<ILogStore>();
            _appInsightsApi = ServiceLocator.ServiceProvider.GetService<IAppInsightsApi>();

        }
        public void Run(List<Item> items, string taskName)
        {
            foreach (var item in items)
            {
                var applicationId = item.ID.ToString();
                var application = _logStore.GetApplication(applicationId);
                if (application == null) application = CreateApplication(item);
                if (application == null) continue;
                if (application.NextHourly < DateTime.Now)
                {
                    Sitecore.Diagnostics.Log.Info("App Insights Import Task: NextHourly set to " + DateTime.Now.AddHours(1.0).ToShortDateString(), this);
                    _logStore.SetNextHourly(applicationId, DateTime.Now.AddHours(1.0));
                    _appInsightsApi.GetGroupedExceptions(application.ApplicationInsightsId, application.ApplicationInsightsKey, "1hr")
                        .ForEach(x => _logStore.AddGroupedException(applicationId, x, AppInsightType.Hourly));
                }
                if (application.NextDaily < DateTime.Now)
                {
                    Sitecore.Diagnostics.Log.Info("App Insights Import Task: NextDaily", this);
                    _logStore.SetNextDaily(applicationId, DateTime.Now.AddHours(24.0));
                    RetrieveLogsAndSaveToStore(applicationId, application, "24hr");
                    Sitecore.Diagnostics.Log.Info("App Insights Import Task: NextDaily set to "+ DateTime.Now.AddHours(24.0).ToShortDateString(), this);
                }
            }
        }

        private void RetrieveLogsAndSaveToStore(string applicationId, Application application, string timespan="1hr")
        {
            foreach (var groupedException in _appInsightsApi.GetGroupedExceptions(application.ApplicationInsightsId, application.ApplicationInsightsKey, timespan))
            {
                _logStore.AddGroupedException(applicationId, groupedException, timespan=="1hr" ? AppInsightType.Hourly : AppInsightType.Daily);
            }
        }

        private Application CreateApplication(Item item)
        {
            if (
                item != null &&
                checkFieldExistsAndNotNull(item, "Title") &&
                checkFieldExistsAndNotNull(item, "ApplicationInsightsId") &&
                checkFieldExistsAndNotNull(item, "ApplicationInsightsKey")
                ) return _logStore.AddApplication(item.ID.ToString(), item.Fields["Title"].Value, item.Fields["ApplicationInsightsId"].Value, item.Fields["ApplicationInsightsKey"].Value);
            return null;
        }
        private bool checkFieldExistsAndNotNull(Item item, string fieldName) {
            if (item.Fields[fieldName] != null && !string.IsNullOrWhiteSpace(item.Fields[fieldName].Value)) return true;
            return false;
        }
    }

}