using Azure.Messaging.ServiceBus.Administration;
using DeanOBrien.Feature.ApplicationInsights.Extensions;
using DeanOBrien.Feature.ApplicationInsights.Models;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights.ApplicationInsights;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;
using ICSharpCode.SharpZipLib.Checksum;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Masters;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using static Sitecore.Configuration.Settings;

namespace DeanOBrien.Feature.ApplicationInsights.Utilities
{
    public static class AlertsHelper
    {
        private static IAppInsightsApi _appInsightsApi;
        private static ILogStore _logStore;
        private static IGenAIService _genAIService;

        private static string EntraClientID { get; set; }

        private static string EntraClientSecret { get; set; }
        private static string TenantID { get; set; }
        private static Database _master;
        private const string ApplicationsRootID = "{3192E2CD-5C42-4EB7-8348-66E887469CD2}";

        static AlertsHelper()
        {
            Log.Info("AlertsHelper() start", "AlertsHelper");
            _appInsightsApi = ServiceLocator.ServiceProvider.GetService<IAppInsightsApi>();
            _logStore = ServiceLocator.ServiceProvider.GetService<ILogStore>();
            _genAIService = ServiceLocator.ServiceProvider.GetService<IGenAIService>();


            _master = Sitecore.Configuration.Factory.GetDatabase("master");

            var applicationInsightSettings = _master.GetItem(ApplicationsRootID);
            EntraClientID = applicationInsightSettings.Fields["Entra Client ID"].Value;
            EntraClientSecret = applicationInsightSettings.Fields["Entra Client Secret"].Value;
            TenantID = applicationInsightSettings.Fields["Tenant Id"].Value;
            _appInsightsApi.Initialize(EntraClientID, EntraClientSecret, TenantID);
            Log.Info("AlertsHelper() end", "AlertsHelper");
        }
        public static List<Alert> GetAlerts(Item item)
        { 
            var response = new List<Alert>();
            var alertsContainer = item.Children.Where(i => i.TemplateName=="App Insight Alerts Folder").FirstOrDefault();
            if (alertsContainer != null) {
                var alertItems = alertsContainer.Children.Where(a => a.ImplementsTemplate("App Insight Alert")).ToList();
                foreach ( var alertItem in alertItems) 
                {
                    var alert = new Alert();
                    alert.ApplicationId = item.ID.ToString();
                    alert.NextRun = string.IsNullOrWhiteSpace(alertItem.Fields["Next Run"].Value) ? DateTime.MinValue : Sitecore.DateUtil.IsoDateToDateTime(alertItem.Fields["Next Run"].Value);
                    alert.Enabled = alertItem?.Fields["Enabled"]?.Value == "1" ? true : false;
                    if (alertItem.TemplateName == "Exception Contains String")
                    {
                        if (alertItem.Fields["Exception String"] == null || alertItem.Fields["Threshold"] == null) continue;

                        alert.AlertType = AlertType.ExceptionContainsString;
                        alert.ExceptionString = alertItem.Fields["Exception String"].Value;
                        alert.Threshold = System.Convert.ToInt32(string.IsNullOrWhiteSpace(alertItem.Fields["Threshold"].Value) ? "0" : alertItem.Fields["Threshold"].Value);
                    }
                    if (alertItem.TemplateName == "Exception Spike")
                    {
                        if (alertItem.Fields["Percentage Threshold"] == null) continue;

                        alert.AlertType = AlertType.ExceptionSpike;
                        alert.PercentageThreshold = System.Convert.ToInt32(string.IsNullOrWhiteSpace(alertItem.Fields["Percentage Threshold"].Value) ? "0" : alertItem.Fields["Percentage Threshold"].Value);
                    }
                    if (alertItem.TemplateName == "Schedule Task Inactivity" || alertItem.TemplateName == "Cron Task Inactivity")
                    {
                        if (alertItem.Fields["Inactivity Threshold in Mins"] == null) continue;

                        if (alertItem.TemplateName == "Cron Task Inactivity")
                        {
                            if (alertItem.Fields["Linked Cron Task"] == null) continue;
                            alert.AlertType = AlertType.CronTaskInactivity;
                            alert.LinkedItem = string.IsNullOrWhiteSpace(alertItem.Fields["Linked Cron Task"].Value) ? null : alertItem.GetLinkedItem("Linked Cron Task");
                        }
                        if (alertItem.TemplateName == "Schedule Task Inactivity")
                        {
                            if (alertItem.Fields["Linked Scheduled Task"] == null) continue;
                            alert.AlertType = AlertType.ScheduledTaskInactivity;
                            alert.LinkedItem = string.IsNullOrWhiteSpace(alertItem.Fields["Linked Scheduled Task"].Value) ? null : alertItem.GetLinkedItem("Linked Scheduled Task");
                        }
                        alert.InactivityThresholdInMins = System.Convert.ToInt32(string.IsNullOrWhiteSpace(alertItem.Fields["Inactivity Threshold in Mins"].Value) ? "0" : alertItem.Fields["Inactivity Threshold in Mins"].Value);
                    }
                    if (alertItem.TemplateName == "Custom Event")
                    {
                        if (alertItem.Fields["Hours Since Custom Event"] == null || alertItem.Fields["Custom Event"] == null) continue;

                        alert.AlertType = AlertType.CustomEvent;
                        alert.CustomEvent = alertItem.Fields["Custom Event"].Value;
                        alert.EventDesired = (!string.IsNullOrWhiteSpace(alertItem.Fields["Event Desired"].Value) && alertItem.Fields["Event Desired"].Value == "1") ? true : false;
                        alert.HoursSinceCustomEvent = System.Convert.ToInt32(string.IsNullOrWhiteSpace(alertItem.Fields["Hours Since Custom Event"].Value) ? "0" : alertItem.Fields["Hours Since Custom Event"].Value);
                    }
                    if (alertItem.TemplateName == "Webpage Down")
                    {
                        if (alertItem.Fields["Url"] == null) continue;

                        alert.AlertType = AlertType.WebpageDown;
                        alert.Url = alertItem.Fields["Url"]?.Value;
                    }
                    if (alertItem.TemplateName == "AI Alert")
                    {
                        alert.AlertType = AlertType.AIAlert;
                    }
                    if (alertItem.TemplateName == "Service Bus Queue Exceeds")
                    {
                        if (alertItem.Fields["Service Bus Queue"] == null) continue;
                        if (alertItem.Fields["Service Bus Connection"] == null) continue;
                        if (alertItem.Fields["Service Bus Queue Limit"] == null) continue;

                        alert.AlertType = AlertType.ServiceBusQueueExceeds;
                        alert.ServiceBusQueue = alertItem.Fields["Service Bus Queue"]?.Value;
                        alert.ServiceBusConnection = alertItem.Fields["Service Bus Connection"]?.Value;
                        alert.ServiceBusQueueLimit = System.Convert.ToInt32(string.IsNullOrWhiteSpace(alertItem.Fields["Service Bus Queue Limit"].Value) ? "0" : alertItem.Fields["Service Bus Queue Limit"].Value);
                    }
                    alert.Item = alertItem;
                    alert.DelayBetweenAlerts = System.Convert.ToInt32(string.IsNullOrWhiteSpace(alertItem.Fields["Delay Between Alerts"].Value) ? "0" : alertItem.Fields["Delay Between Alerts"].Value);
                    alert.Title = alertItem.DisplayName;
                    alert.Subscribers = alertItem.Children.Where(a => a.TemplateName == "Subscriber").Select(s => new Subscriber() { Email = s.Fields["Email"].Value }).ToList();
                    response.Add(alert);
                }
            }
            return response;
        }

        internal static Tuple<bool,string> CheckIfAlertShouldBeTriggered(List<GroupedException> exceptions, Alert alert, Application application)
        {
            if (alert.NextRun > DateTime.Now) return new Tuple<bool, string>(false, "");
            if (!alert.Enabled) return new Tuple<bool, string>(false, "");
            if (alert.AlertType == AlertType.ExceptionContainsString)
            {
                foreach (var groupedExceptionInLastHour in exceptions)
                {
                    if (((groupedExceptionInLastHour.InnerMostMessage != null && groupedExceptionInLastHour.InnerMostMessage.Contains(alert.ExceptionString))
                    || (groupedExceptionInLastHour.OuterMessage != null && groupedExceptionInLastHour.OuterMessage.Contains(alert.ExceptionString))
                    || (groupedExceptionInLastHour.ProblemId != null && groupedExceptionInLastHour.ProblemId.Contains(alert.ExceptionString))
                    || (groupedExceptionInLastHour.Type != null && groupedExceptionInLastHour.Type.Contains(alert.ExceptionString))
                    || (groupedExceptionInLastHour.InnerMostType != null && groupedExceptionInLastHour.InnerMostType.Contains(alert.ExceptionString))
                    || (groupedExceptionInLastHour.OuterType != null && groupedExceptionInLastHour.OuterType.Contains(alert.ExceptionString))
                    || (groupedExceptionInLastHour.OuterAssembly != null && groupedExceptionInLastHour.OuterAssembly.Contains(alert.ExceptionString)))
                    && groupedExceptionInLastHour.Count >= alert.Threshold) return new Tuple<bool, string>(true,"");
                }
            }
            else if (alert.AlertType == AlertType.ExceptionSpike)
            {
                var applicatonID = application.Id.ToString().Contains("{") ? application.Id.ToString() : "{" + application.Id.ToString() + "}";
                var totalExceptionsInLastHour = exceptions.Select(e => e.Count).Sum();
                var logStore = ServiceLocator.ServiceProvider.GetService<ILogStore>();
                var dailyLogs = logStore.GetGroupedExceptions(applicatonID, null, AppInsightType.Daily, "7d");
                var hourlyLogs = logStore.GetGroupedExceptions(applicatonID, null, AppInsightType.Hourly, "7d");
                var summaryOfExceptions = new SummaryOfExceptions(applicatonID, "7d", dailyLogs, hourlyLogs);
                var highestNumberExceptionsInHour = summaryOfExceptions.HourlySummary.Max(x => x.Count);
                if (alert.PercentageThreshold == 0) return new Tuple<bool, string>(false, "");

                double limit = highestNumberExceptionsInHour + (highestNumberExceptionsInHour * (double)alert.PercentageThreshold / 100);
                if (totalExceptionsInLastHour > highestNumberExceptionsInHour) return new Tuple<bool, string>(true, "");
            }
            else if (alert.AlertType == AlertType.CustomEvent)
            {
                if (alert.HoursSinceCustomEvent == 0) return new Tuple<bool, string>(false, "");
                if (string.IsNullOrWhiteSpace(alert.CustomEvent)) return new Tuple<bool, string>(false, "");

                var customEvents = _appInsightsApi.GetCustomEventsV2(application.ApplicationInsightsId, alert.CustomEvent, $"{alert.HoursSinceCustomEvent}h");
                if ((customEvents.Count > 0 && !alert.EventDesired) || (customEvents.Count == 0 && alert.EventDesired)) return new Tuple<bool, string>(true, "");
                return new Tuple<bool, string>(false, "");
            }
            else if (alert.AlertType == AlertType.CronTaskInactivity)
            {
                if (alert.LinkedItem == null) return new Tuple<bool, string>(false, "");
                if (alert.InactivityThresholdInMins <= 0) return new Tuple<bool, string>(false, "");
                if (alert.LinkedItem.Fields["LastRunUTC"] == null || string.IsNullOrWhiteSpace(alert.LinkedItem.Fields["LastRunUTC"]?.Value)) return new Tuple<bool, string>(true, "");

                var lastRun = alert.LinkedItem.Fields["LastRunUTC"]?.Value;
                var lastRunAsDateTime = DateUtil.IsoDateToDateTime(lastRun);

                if (lastRunAsDateTime < DateTime.Now.AddMinutes((double)(-1 * alert.InactivityThresholdInMins))) return new Tuple<bool, string>(true, "");
                return new Tuple<bool, string>(false, "");
            }
            else if (alert.AlertType == AlertType.ScheduledTaskInactivity)
            {
                if (alert.LinkedItem == null) return new Tuple<bool, string>(false, "");
                if (alert.InactivityThresholdInMins <= 0) return new Tuple<bool, string>(false, "");
                if (alert.LinkedItem.Fields["Last run"] == null || string.IsNullOrWhiteSpace(alert.LinkedItem.Fields["Last run"]?.Value)) return new Tuple<bool, string>(true, "");

                var lastRun = alert.LinkedItem.Fields["Last run"]?.Value;
                var lastRunAsDateTime = DateUtil.IsoDateToDateTime(lastRun);

                if (lastRunAsDateTime < DateTime.Now.AddMinutes((double)(-1 * alert.InactivityThresholdInMins))) return new Tuple<bool, string>(true, "");
                return new Tuple<bool, string>(false, "");
            }
            else if (alert.AlertType == AlertType.WebpageDown)
            {
                if (string.IsNullOrWhiteSpace(alert.Url)) return new Tuple<bool, string>(false, "");
                if (GetHeaders(alert.Url) != HttpStatusCode.OK) return new Tuple<bool, string>(true, "");
                return new Tuple<bool, string>(false, "");
            }
            else if (alert.AlertType == AlertType.ServiceBusQueueExceeds)
            {
                var client = new ServiceBusAdministrationClient(alert.ServiceBusConnection);
                var queue = client.GetQueueRuntimePropertiesAsync(alert.ServiceBusQueue);
                if ((int)queue.Result.Value.TotalMessageCount > alert.ServiceBusQueueLimit) return new Tuple<bool, string>(true, "");
            }
            else if (alert.AlertType == AlertType.AIAlert)
            {
                var healthCheck = GetHealthCheck(application.Id.ToString(), "30d");
                if (healthCheck.Contains("APPLICATIONHEALTHY")) return new Tuple<bool, string>(false, "");
                return new Tuple<bool, string>(true, healthCheck);
            }

            if (alert.AlertType == AlertType.ExceptionContainsString)
            {
                //if (((exception.InnerMostMessage != null && exception.InnerMostMessage.Contains(alert.ExceptionString))
                //    || (exception.OuterMessage != null && exception.OuterMessage.Contains(alert.ExceptionString))
                //    || (exception.ProblemId != null && exception.ProblemId.Contains(alert.ExceptionString))
                //    || (exception.Type != null && exception.Type.Contains(alert.ExceptionString))
                //    || (exception.InnerMostType != null && exception.InnerMostType.Contains(alert.ExceptionString))
                //    || (exception.OuterType != null && exception.OuterType.Contains(alert.ExceptionString))
                //    || (exception.OuterAssembly != null && exception.OuterAssembly.Contains(alert.ExceptionString)))
                //    && exception.Count >= alert.Threshold) return true;
            }
            else if (alert.AlertType == AlertType.ExceptionSpike)
            {
                //var applicatonID = application.Id.ToString().Contains("{") ? application.Id.ToString() : "{" + application.Id.ToString() + "}";
                //var totalExceptionsInLastHour = exceptions.Select(e => e.Count).Sum();                // Cache this for an hour?
                //var logStore = ServiceLocator.ServiceProvider.GetService<ILogStore>();
                //var dailyLogs = logStore.GetGroupedExceptions(applicatonID, null, AppInsightType.Daily, "7d");
                //var hourlyLogs = logStore.GetGroupedExceptions(applicatonID, null, AppInsightType.Hourly, "7d");
                //var summaryOfExceptions = new SummaryOfExceptions(applicatonID, "7d", dailyLogs, hourlyLogs);
                //var highestNumberExceptionsInHour = summaryOfExceptions.HourlySummary.Max(x => x.Count);
                //if (alert.PercentageThreshold == 0) return false;

                //double limit = highestNumberExceptionsInHour + (highestNumberExceptionsInHour * (double)alert.PercentageThreshold / 100);
                //if (totalExceptionsInLastHour > highestNumberExceptionsInHour) return true;
            }
            else if (alert.AlertType == AlertType.CronTaskInactivity || alert.AlertType == AlertType.ScheduledTaskInactivity)
            {
                //if (alert.LinkedItem == null) return false;
                //if (alert.InactivityThresholdInMins <= 0) return false;
                //if (alert.AlertType == AlertType.CronTaskInactivity && alert.LinkedItem.Fields["LastRunUTC"] == null || string.IsNullOrWhiteSpace(alert.LinkedItem.Fields["LastRunUTC"].Value)) return true;
                //if (alert.AlertType == AlertType.ScheduledTaskInactivity && alert.LinkedItem.Fields["Last run"] == null || string.IsNullOrWhiteSpace(alert.LinkedItem.Fields["Last run"].Value)) return true;

                //var lastRun = alert.AlertType == AlertType.CronTaskInactivity ? alert.LinkedItem.Fields["LastRunUTC"].Value : alert.LinkedItem.Fields["Last run"].Value;

                //DateTime lastRunAsDateTime = DateTime.MinValue;
                //DateTime.TryParse(lastRun, out lastRunAsDateTime);

                //if (lastRunAsDateTime < DateTime.Now.AddMinutes((double)(-1 * alert.InactivityThresholdInMins))) return true;
                //return false;
            }
            else if (alert.AlertType == AlertType.CustomEvent)
            {
                //if (alert.HoursSinceCustomEvent == 0) return false;
                //if (string.IsNullOrWhiteSpace(alert.CustomEvent)) return false;
                //if ((customEvents.Count > 0 && !alert.EventDesired) || (customEvents.Count == 0 && alert.EventDesired)) return true;
                //return false;
            }
            else if (alert.AlertType == AlertType.WebpageDown)
            {
                //if (string.IsNullOrWhiteSpace(alert.Url)) return false;
                //if (GetHeaders(alert.Url) != HttpStatusCode.OK) return true;
                //return false;
            }
            return new Tuple<bool, string>(false, "");
        }

        internal static void NotifySubscribers(Alert alert, string additionalMessage="")
        {
            string message = GetFriendlyMessage(alert, additionalMessage);
            if (alert.Subscribers != null && alert.Subscribers.Count() > 0)
            {
                foreach (var subscriber in alert.Subscribers)
                {
                    SendEmail(subscriber.Email, message, "Alert from sitecore");
                }
            }
        }

        private static string GetFriendlyMessage(Alert alert, string additionalMessage="")
        {
            var message = string.Empty;

            if (alert.AlertType == AlertType.ExceptionContainsString) message = $"The alert '{alert.Title}' has been triggered, because the string '{alert.ExceptionString}' was found in multiple exceptions in the last hour. This exceed the threshold of '{alert.Threshold}' which has been configured.";
            if (alert.AlertType == AlertType.ExceptionSpike) message = $"The alert '{alert.Title}' has been triggered, because exceptions in the last hour exceeds the '{alert.PercentageThreshold}' percent spike threshold of which has been configured.";
            if (alert.AlertType == AlertType.CronTaskInactivity || alert.AlertType == AlertType.ScheduledTaskInactivity) message = $"The alert '{alert.Title}' has been triggered, because there has been no activity in the last {alert.InactivityThresholdInMins} minutes.";
            if (alert.AlertType == AlertType.CustomEvent && alert.EventDesired) message = $"The alert '{alert.Title}' has been triggered because a custom event was not found containing '{alert.CustomEvent}' in the last {alert.HoursSinceCustomEvent}.";
            if (alert.AlertType == AlertType.CustomEvent && !alert.EventDesired) message = $"The alert '{alert.Title}' has been triggered because a custom event was found containing '{alert.CustomEvent}' in the last {alert.HoursSinceCustomEvent}.";
            if (alert.AlertType == AlertType.WebpageDown) message = $"The alert '{alert.Title}' has been triggered, because the url'{alert.Url}' did not return a 200 status.";
            if (alert.AlertType == AlertType.AIAlert) message = $"The alert '{alert.Title}' has been triggered. \r\n {additionalMessage}";

            return message;
        }

        internal static void SetNextRun(Item item, DateTime dateTime)
        {
            try {
                const string adminUser = @"sitecore\Admin";
                if (Sitecore.Security.Accounts.User.Exists(adminUser))
                {
                    var scUser = Sitecore.Security.Accounts.User.FromName(adminUser, true);
                    using (new Sitecore.Security.Accounts.UserSwitcher(scUser))
                    {
                        item.Editing.BeginEdit();
                        var isoDate = DateUtil.ToIsoDate(dateTime);
                        item["Next Run"] = isoDate;
                        item.Editing.EndEdit();
                    }
                }
            }
            catch(Exception ex) {
                Log.Info($"Problem updating Next Run: {ex.Message}", "AlertsHelper");
            }
        }
        private static void SendEmail(string email, string message, string subject)
        {
            if (string.IsNullOrWhiteSpace(email))
                return;
            MailMessage message1 = new MailMessage();
            string address = "website@livenorthumbriaac.onmicrosoft.com";
            message1.To.Add(email);
            message1.From = new MailAddress(address);
            message1.Subject = subject;
            message1.IsBodyHtml = true;
            string str = string.Format("<p>{0}</p>", (object)message);
            message1.Body = str;
            new SmtpClient().Send(message1);
        }
        private static HttpStatusCode GetHeaders(string url)
        {
            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Method = "HEAD";
            try
            {
                using (HttpWebResponse response = webRequest.GetResponse() as HttpWebResponse)
                {
                    if (response != null)
                    {
                        int statusCode = (int)response.StatusCode;
                        response.Close();
                        return (HttpStatusCode)statusCode;
                    }
                }
            }
            catch
            {
                return HttpStatusCode.BadRequest;
            }
            return HttpStatusCode.BadRequest;
        }
        private static string GetHealthCheck(string id, string timespan = "30d")
        {
            string error = string.Empty;
            if (id == null)
            {
                return null;
            }
            var application = _master.GetItem(id);
            if (application == null)
            {
                var applications = _master.GetItem(ApplicationsRootID);
                application = applications.GetChildren().Where(x => x.Fields["Title"].Value == "CM").FirstOrDefault();
                if (application == null) application = applications.GetChildren().FirstOrDefault();
                if (application == null) return null;
            }

            var appInsightsId = application.Fields["ApplicationInsightsId"].Value;
            var applicationId = application.ID.ToString();
            if (!id.Contains("{")) id = "{" + id + "}";

            try
            {
                List<GroupedException> combinedHourlyLogs = GetExceptionsFromApiAndDB(timespan, applicationId, appInsightsId);
                int minsSince = GetTimeSinceLastHourly(applicationId);

                var viewModel = new SummaryForAIOverview(id, timespan, combinedHourlyLogs);
                viewModel.Application = new Application() { Id = application.ID.ToGuid(), Title = application.Fields["Title"].Value };
                viewModel.Application.ApplicationInsightsId = application.Fields["ApplicationInsightsId"].Value;

                var exceptionsInTimeSpanAsJson = JsonConvert.SerializeObject(viewModel.ExceptionsInTimeSpan);

                var prompt = new List<Tuple<string, string>>() {
                        new Tuple<string,string>("System","You are a IT assistant, with in depth knowledge of C# programming language."),
                        new Tuple<string,string>("User",$"Please consider the following JSON enclosed by *** which represents the exceptions found in an application over the last {viewModel.TimeSpan} hours and {minsSince} minutes. Using the provided data, please advise if you see anything out of the unusual in the last 3 hours. ***{exceptionsInTimeSpanAsJson}***"),
                        new Tuple<string,string>("User",$"Please respond with one a detailed analysis of the new trend."),
                        new Tuple<string,string>("User",$"Please ensure your response uses well formed HTML. Do not use h1 or h2 header tags."),
                        new Tuple<string,string>("User",$"The response should be formatted using HTML tags. The entire response should be valid HTML."),
                        new Tuple<string,string>("User",$"There should be no backticks (i.e. `) in the response."),
                        new Tuple<string,string>("User",$"If nothing unusual is found in the data respond with the message 'APPLICATIONHEALTHY'."),

                    };

                string generatedInsight = _genAIService.Call(prompt, "", "");

                return generatedInsight;
            }
            catch (Exception ex)
            {
                return $"<p>There was a problem generating the AI Overview.</p><p><i>{ex.Message}</i></p>";
            }
        }
        private static List<GroupedException> GetExceptionsFromApiAndDB(string timespan, string applicationID, string appInsightsId)
        {
            var appFromSQL = _logStore.GetApplication(applicationID);
            int minutesToGetFromApi = 60 - appFromSQL.NextHourly.Subtract(DateTime.Now).Minutes;
            var latestLogsFromApi = new List<GroupedException>();
            latestLogsFromApi = _appInsightsApi.GetGroupedExceptionsV2(appInsightsId, $"{minutesToGetFromApi}m").ToList();
            foreach (var item in latestLogsFromApi)
            {
                item.DateCreated = appFromSQL.NextHourly;
            }
            var hourlyLogsFromDB = _logStore.GetGroupedExceptions(applicationID, AppInsightType.Hourly, timespan).ToList();
            var combined = hourlyLogsFromDB.Concat(latestLogsFromApi).ToList();
            return combined;
        }
        private static int GetTimeSinceLastHourly(string applicationId)
        {
            var appFromSQL = _logStore.GetApplication(applicationId);
            return 60 - appFromSQL.NextHourly.Subtract(DateTime.Now).Minutes;
        }
        internal static void LogTriggeredAlert(Alert alert)
        {
            string message = GetFriendlyMessage(alert);
            _logStore.AddTriggeredAlert(alert.ApplicationId, alert.Item.ID.ToString(), message, DateTime.Now);
        }
    }
}
