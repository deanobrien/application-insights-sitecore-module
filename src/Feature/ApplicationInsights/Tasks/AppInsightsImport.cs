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
using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;
using Sitecore.Data.Masters;
using Sitecore.Data;
using Sitecore.Diagnostics;
using DeanOBrien.Feature.ApplicationInsights.Utilities;
using DeanOBrien.Feature.ApplicationInsights.Models;

namespace DeanOBrien.Feature.ApplicationInsights.Tasks
{
    public class AppInsightsImport
    {
        private ILogStore _logStore;
        private IAppInsightsApi _appInsightsApi;
        private Database _master;
        private const string ApplicationsRootID = "{3192E2CD-5C42-4EB7-8348-66E887469CD2}";
        private string EntraClientID { get; set; }
        private string EntraClientSecret { get; set; }
        private string TenantID { get; set; }

        public AppInsightsImport()
        {
            Log.Info("AppInsightsImport() start",this);
            _logStore = ServiceLocator.ServiceProvider.GetService<ILogStore>();
            _appInsightsApi = ServiceLocator.ServiceProvider.GetService<IAppInsightsApi>();
            _master = Sitecore.Configuration.Factory.GetDatabase("master");

            var applicationInsightSettings = _master.GetItem(ApplicationsRootID);
            EntraClientID = applicationInsightSettings.Fields["Entra Client ID"].Value;
            EntraClientSecret = applicationInsightSettings.Fields["Entra Client Secret"].Value;
            TenantID = applicationInsightSettings.Fields["Tenant Id"].Value;
            _appInsightsApi.Initialize(EntraClientID, EntraClientSecret, TenantID);
            Log.Info("AppInsightsImport() end",this);
        }
        public void Run(List<Item> items, string taskName)
        {
            Log.Info("Run() start", this);
            foreach (var item in items)
            {
                Log.Info($"Run() foreach {item.ID}", this);
                var applicationId = item.ID.ToString();
                var application = _logStore.GetApplication(applicationId);
                if (application == null) application = CreateApplication(item);
                if (application == null) continue;


                var groupedExceptionsInLastHour = _appInsightsApi.GetGroupedExceptionsV2(application.ApplicationInsightsId, "1hr");

                var alerts = AlertsHelper.GetAlerts(item);
                if (alerts.Count() > 0)
                { 
                    foreach (var alert in alerts)
                    {
                        var alertCheck = AlertsHelper.CheckIfAlertShouldBeTriggered(groupedExceptionsInLastHour, alert, application);
                        if (alertCheck.Item1)
                        {
                            AlertsHelper.SetNextRun(alert.Item, DateTime.Now.AddMinutes((int)(alert.DelayBetweenAlerts ?? 1)));
                            AlertsHelper.NotifySubscribers(alert, alertCheck.Item2);
                            AlertsHelper.LogTriggeredAlert(alert);

                            continue;

                        }
                        if (alert.NextRun <= DateTime.Now) AlertsHelper.SetNextRun(alert.Item, DateTime.Now);
                    }
                }

                if (application.NextHourly < DateTime.Now)
                {
                    Sitecore.Diagnostics.Log.Info("App Insights Import Task: NextHourly set to " + DateTime.Now.AddHours(1.0).ToShortDateString(), this);
                    _logStore.SetNextHourly(applicationId, DateTime.Now.AddHours(1.0));
                    foreach (var groupedException in groupedExceptionsInLastHour)
                    {
                        _logStore.AddGroupedException(applicationId, groupedException, AppInsightType.Hourly);
                    }
                }
                if (application.NextDaily < DateTime.Now)
                {
                    Sitecore.Diagnostics.Log.Info("App Insights Import Task: NextDaily", this);
                    _logStore.SetNextDaily(applicationId, DateTime.Now.AddHours(24.0));
                    RetrieveLogsAndSaveToStore(applicationId, application, "24hr");
                    Sitecore.Diagnostics.Log.Info("App Insights Import Task: NextDaily set to "+ DateTime.Now.AddHours(24.0).ToShortDateString(), this);
                }
            }
            Log.Info("Run() end", this);
        }

        private void RetrieveLogsAndSaveToStore(string applicationId, Application application, string timespan="1hr")
        {
            foreach (var groupedException in _appInsightsApi.GetGroupedExceptionsV2(application.ApplicationInsightsId, timespan))
            {
                _logStore.AddGroupedException(applicationId, groupedException, timespan=="1hr" ? AppInsightType.Hourly : AppInsightType.Daily);
            }
        }

        private Application CreateApplication(Item item)
        {
            if (
                item != null &&
                checkFieldExistsAndNotNull(item, "Title") &&
                checkFieldExistsAndNotNull(item, "ApplicationInsightsId")
                ) return _logStore.AddApplication(item.ID.ToString(), item.Fields["Title"].Value, item.Fields["ApplicationInsightsId"].Value, "n/a");
            return null;
        }
        private bool checkFieldExistsAndNotNull(Item item, string fieldName) {
            if (item.Fields[fieldName] != null && !string.IsNullOrWhiteSpace(item.Fields[fieldName].Value)) return true;
            return false;
        }
    }

}