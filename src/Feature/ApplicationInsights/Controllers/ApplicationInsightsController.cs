using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Sitecore.Data;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;
using DeanOBrien.Feature.ApplicationInsights.Models;
using DeanOBrien.Feature.ApplicationInsights.Extensions;
using Sitecore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Diagnostics;
using System.Runtime.InteropServices;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights.ApplicationInsights;
using System.Text;
using System.Diagnostics;
using DeanOBrien.Feature.ApplicationInsights.Utilities;
using Newtonsoft.Json;
using Sitecore.Data.Items;
using Sitecore.Web.UI.HtmlControls;
using System.Text.RegularExpressions;
using System.Buffers.Text;
using System.Web.Http.Routing.Constraints;

namespace DeanOBrien.Feature.ApplicationInsights.Controllers
{
    public class ApplicationInsightsController : Controller
    {
        private IAppInsightsApi _appInsightsApi;
        private ILogStore _logStore;
        private IGenAIService _genAIService;
        private Database _master;
        private ITfsService _tfsService;
        private const string ApplicationsRootID = "{3192E2CD-5C42-4EB7-8348-66E887469CD2}";

        public ApplicationInsightsController(IAppInsightsApi appInsightsApi, ILogStore logStore, IGenAIService genAIService, ITfsService tfsService)
        {
            _appInsightsApi = appInsightsApi;
            _tfsService = tfsService;
            _logStore = logStore;
            _genAIService = genAIService;
            _master = Sitecore.Configuration.Factory.GetDatabase("master");

            var applicationInsightSettings = _master.GetItem(ApplicationsRootID);
            var entraClientID = applicationInsightSettings.Fields["Entra Client ID"].Value;
            var entraClientSecret = applicationInsightSettings.Fields["Entra Client Secret"].Value;
            var tenantID = applicationInsightSettings.Fields["Tenant Id"].Value;
            _appInsightsApi.Initialize(entraClientID, entraClientSecret, tenantID);

            var organization = applicationInsightSettings.Fields["Tfs Organization"].Value;
            var project = applicationInsightSettings.Fields["Tfs Project"].Value;
            var repoId = applicationInsightSettings.Fields["Tfs Repository Id"].Value;
            var pat = applicationInsightSettings.Fields["Tfs Pat Token"].Value;
            _tfsService.Initialize(organization, project, repoId, pat);
        }

        /*
        // Used to populate Graph and Dependencies
        // Get exceptions from DB and top up from API <= only this approach gives data for graph
        // This ensure the very latest data
        */
        public ActionResult Index(string id, string problemIdBase64 = null, string innerMostMessageBase64 = null, string timespan = "7d")
        {
            string error = string.Empty;
            if (id == null)
            {
                return HttpNotFound();
            }
            var application = _master.GetItem(id);
            if (application == null)
            {
                var applications = _master.GetItem(ApplicationsRootID);
                application = applications.GetChildren().Where(x => x.Fields["Title"].Value == "CM").FirstOrDefault();
                if (application == null) application = applications.GetChildren().FirstOrDefault();
                if (application == null) return HttpNotFound();
            }
            var appInsightsId = application.Fields["ApplicationInsightsId"].Value;
            var applicationId = application.ID.ToString();
            var viewModel = GetDependencies(application);
            viewModel.StackTraceVariations = new List<Tuple<string, string, int, int, string>>();
            if (problemIdBase64 != null)
            {
                viewModel.ProblemIdBase64 = problemIdBase64;
                var problemId = DecodeBase64(problemIdBase64);
                problemId = RemoveSpecialCharacters(problemId);
                problemId = problemId.Replace(" ", "");

                viewModel.WorkItemId = _tfsService.GetWorkItemId(problemId).Result;

                string workItemDescription = string.Empty;
                if (viewModel.WorkItemId != 0) viewModel.AnalysisFromWorkItem = _tfsService.GetWorkItemDescription(viewModel.WorkItemId).Result;


                int timespanInMins = GetAjustedTimeSpanInMinutes(timespan, applicationId);

                var exceptions = _appInsightsApi.GetSingleException(appInsightsId, problemIdBase64, innerMostMessageBase64, $"{timespanInMins}m");

                var stackTraceVariations = new List<Tuple<string, string, int, int, string>>();
                foreach (var item in exceptions)
                {
                    var systemPrompt = $"Please consider the stack trace enclosed by *** and offer a suggestion on how to fix the problem. ***{item.Details}***";

                    if (!string.IsNullOrWhiteSpace(item.Path) && item.Line > 0)
                    {
                        systemPrompt = $"Please consider the exception enclosed by ***, which refers to line {item.Line} of the code enclosed by +++ and offer a suggestion on how to fix the problem. ***{item.Details}***.";
                    }
                    var systemPromptBase64 = Base64Encode(systemPrompt);


                    if (stackTraceVariations.Where(x => x.Item1 == item.Details && x.Item2 == item.Path && x.Item3 == item.Line).FirstOrDefault() == null)
                    {
                        stackTraceVariations.Add(new Tuple<string, string, int, int, string>(item.Details, item.Path, item.Line, 1, systemPromptBase64));
                        continue;
                    }
                    var index = stackTraceVariations.FindIndex(x => x.Item1 == item.Details && x.Item2 == item.Path && x.Item3 == item.Line);
                    stackTraceVariations[index] = Tuple.Create(item.Details, item.Path, item.Line, stackTraceVariations[index].Item4 + 1, systemPromptBase64);
                }
                viewModel.StackTraceVariations = stackTraceVariations;

                var stackTrace = viewModel.StackTraceVariations?.FirstOrDefault()?.Item1;
            }
            if (innerMostMessageBase64 != null)
            {
                viewModel.InnerMessageBase64 = innerMostMessageBase64;
            }
            viewModel.Application.ApplicationInsightsId = application.Fields["ApplicationInsightsId"].Value;
            viewModel.TimeSpan = timespan;

            if (!id.Contains("{")) id = "{" + id + "}";
            try
            {
                var dailyLogs = _logStore.GetGroupedExceptions(application.ID.ToString(), problemIdBase64, AppInsightType.Daily, timespan);
                if (dailyLogs!=null && dailyLogs.Count > 0)
                {
                    List<GroupedException> combinedHourlyLogs = GetExceptionsFromApiAndDB(problemIdBase64, innerMostMessageBase64, timespan, applicationId, appInsightsId);
                    viewModel.SummaryOfExceptions = new SummaryOfExceptions(id, timespan, dailyLogs, combinedHourlyLogs);
                }
            }
            catch (Exception ex)
            {
                viewModel.ErrorMessage = $"<p>There was a problem getting logs from database, please ensure you have installed ApplicationInsights DacPac and added the <b>ApplicationInsights</b> connection string.</p><p><i>{ex.Message}</i></p>";
            }
            return View("~/sitecore/shell/client/Applications/ApplicationInsights/Index.cshtml", viewModel);
        }

        private string getFileContent(string fileName)
        {
            var file = _tfsService.GetFile(fileName);
            return file.Result;
        }
        private bool CreateBranchAndAddCommit(string newBranch, string fileName, string fileContent)
        {
            var file = _tfsService.CreateBranchAndAddCommit(newBranch, fileName, fileContent);
            return file.Result;
        }
        private int CreateWorkItem(string content, string problemId)
        {
            var file = _tfsService.CreateBacklog(content, problemId);
            return file.Result;
        }
        
        public ActionResult Dependencies(string id = null)
        {
            var applications = _master.GetItem("/sitecore/system/Modules/ApplicationInsights");

            var viewModel = new LinkedApplicationJson();
            viewModel.nodes = new List<LinkedApplicationNode>();
            viewModel.edges = new List<LinkedApplicationEdge>();
            foreach (var application in applications.Children.ToList())
            {
                viewModel.nodes.Add(new LinkedApplicationNode() { id = Strip(application.ID.ToString()), title = application.Fields["Title"].Value, type = "WebApp" });
                application.GetLinkedItems("dependencies").ForEach(x => viewModel.edges.Add(new LinkedApplicationEdge() { source = Strip(x.ID.ToString()), target = Strip(application.ID.ToString()), label = string.Empty, data = new List<LinkedApplicationEdgeData>() }));
            }
            return Json(viewModel, JsonRequestBehavior.AllowGet);
        }

        /*
        // Used to populate left side menu
        // If no problemId, then get exceptions from DB and top up from API
        // If problemId, then need innerMostMessage detail, so get fully from API
        // This ensure the very latest data
        */
        public ActionResult GroupedExceptions(string id, string problemIdBase64 = null, string innerMostMessageBase64 = null, string timespan = "2h")
        {

            if (id == null) return Json(new { ErrorMessage = "Application id missing" }, JsonRequestBehavior.AllowGet);

            var application = _master.GetItem(id);

            if (application == null) return Json(new { ErrorMessage = "Application not found" }, JsonRequestBehavior.AllowGet);
            else if (application.Fields["ApplicationInsightsId"] == null
                || string.IsNullOrWhiteSpace(application.Fields["ApplicationInsightsId"].Value)
                ) return Json(new { ErrorMessage = "ApplicationInsightsId or ApplicationInsightsKey not set" }, JsonRequestBehavior.AllowGet);

            var applicationId = application.ID.ToString();
            var appInsightsId = application.Fields["ApplicationInsightsId"].Value;

            var response = new List<GroupedException>();

            if (string.IsNullOrWhiteSpace(problemIdBase64))
            {
                List<GroupedException> combinedHourlyLogs = GetExceptionsFromApiAndDB(problemIdBase64, innerMostMessageBase64, timespan, applicationId, appInsightsId);

                foreach (var item in combinedHourlyLogs)
                {
                    var problemId = item.ProblemId;
                    var innerMostMessage = item.InnerMostMessage;
                    if (response.Where(x => x.ProblemId == problemId).FirstOrDefault() == null)
                    {
                        response.Add(item);
                        continue;
                    }
                    response.Where(x => x.ProblemId == problemId).FirstOrDefault().Count += item.Count;
                }
            }
            else
            {
                // timespan + mins since last DB recording <= to try and match graph/ai overview
                int timespanInMins = GetAjustedTimeSpanInMinutes(timespan, applicationId);

                // Need to go back to API as we do not store the innerMostMessage in DB
                response = _appInsightsApi.GetGroupedExceptionsV2(appInsightsId, problemIdBase64, innerMostMessageBase64, $"{timespanInMins}m");
            }
            response = response.OrderByDescending(x => x.Count).ToList();


            foreach (var item in response)
            {
                item.ApplicationId = id;
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

  

        public ActionResult GetAlerts(string id, string timespan = "2h")
        {
            if (id == null) return Json(new { ErrorMessage = "Application id missing" }, JsonRequestBehavior.AllowGet);

            if (!id.Contains("{")) id = "{" + id + "}";

            var application = _master.GetItem(id);

            if (application == null) return Json(new { ErrorMessage = "Application not found" }, JsonRequestBehavior.AllowGet);

            int hours = 1;
            if (timespan.Contains("h"))
            {
                int.TryParse(timespan.Replace("h", ""), out hours);
            }
            else if (timespan.Contains("d"))
            {
                int days = 1;
                int.TryParse(timespan.Replace("d", ""), out days);
                hours = days * 24;
            }

            var result = _logStore.GetTriggeredAlerts(id, DateTime.Now.AddHours(-1 * hours));

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /*
        // Used to populate AI Data and Retrieve Summary Of multiple Exceptions Over Time
        // Get exceptions from DB and top up from API <= only this approach gives time data for summary
        // This ensure the very latest data
        */
        public ActionResult GetAISummary(string id, string problemIdBase64 = null, string innerMostMessageBase64 = null, string timespan = "7d", string additional = "")
        {
            string error = string.Empty;
            if (id == null)
            {
                return HttpNotFound();
            }
            var application = _master.GetItem(id);
            if (application == null)
            {
                var applications = _master.GetItem(ApplicationsRootID);
                application = applications.GetChildren().Where(x => x.Fields["Title"].Value == "CM").FirstOrDefault();
                if (application == null) application = applications.GetChildren().FirstOrDefault();
                if (application == null) return HttpNotFound();
            }

            var appInsightsId = application.Fields["ApplicationInsightsId"].Value;
            var applicationId = application.ID.ToString();
            if (!id.Contains("{")) id = "{" + id + "}";

            try
            {
                List<GroupedException> combinedHourlyLogs = GetExceptionsFromApiAndDB(problemIdBase64, innerMostMessageBase64, timespan, applicationId, appInsightsId);
                int minsSince = GetTimeSinceLastHourly(applicationId);

                var viewModel = new SummaryForAIOverview(id, timespan, combinedHourlyLogs);
                viewModel.Application = new Application() { Id = application.ID.ToGuid(), Title = application.Fields["Title"].Value };
                viewModel.Application.ApplicationInsightsId = application.Fields["ApplicationInsightsId"].Value;

                var exceptionsInTimeSpanAsJson = JsonConvert.SerializeObject(viewModel.ExceptionsInTimeSpan);

                var userPrompt = $"Please consider the following JSON enclosed by *** which represents the exceptions found in an application over the last {viewModel.TimeSpan} hours and {minsSince} minutes. Provide an analysis of the data and advise of any patterns found. ***{exceptionsInTimeSpanAsJson}***";
                if (!string.IsNullOrWhiteSpace(additional)) userPrompt = $"Please consider the following JSON enclosed by *** which represents the exceptions found in an application over the last {viewModel.TimeSpan} hours and {minsSince} minutes. {additional}. ***{exceptionsInTimeSpanAsJson}***";

                var prompt = new List<Tuple<string, string>>() {
                        new Tuple<string,string>("System","You are a IT assistant, with in depth knowledge of C# programming language."),
                        new Tuple<string,string>("User",userPrompt),
                        new Tuple<string,string>("User",$"Please ensure your response uses well formed HTML. Do not use h1 or h2 header tags."),
                        new Tuple<string,string>("User",$"Please ensure the HTML used in the response is dynamic and expands to parent. Do not use fixed with elements. Do not add any padding to any elements."),
                        new Tuple<string,string>("User",$"The response should be formatted using HTML tags. The entire response should be valid HTML."),
                        new Tuple<string,string>("User",$"There should be no backticks (i.e. `) in the response.")
                    };

                string generatedInsight = _genAIService.Call(prompt, "", "");

                return Json(generatedInsight, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json($"<p>There was a problem generating the AI Overview.</p><p><i>{ex.Message}</i></p>", JsonRequestBehavior.AllowGet);
            }
        }

        /*
        // Used to populate AI Data and Retrieve Summary Of multiple Exceptions Over Time
        // Get exceptions from DB and top up from API <= only this approach gives time data for summary
        // This ensure the very latest data
        */
        public ActionResult GetAISummaryData(string id, string problemIdBase64 = null, string innerMostMessageBase64 = null, string timespan = "30d")
        {
            string error = string.Empty;
            if (id == null)
            {
                return HttpNotFound();
            }
            var application = _master.GetItem(id);
            if (application == null)
            {
                var applications = _master.GetItem(ApplicationsRootID);
                application = applications.GetChildren().Where(x => x.Fields["Title"].Value == "CM").FirstOrDefault();
                if (application == null) application = applications.GetChildren().FirstOrDefault();
                if (application == null) return HttpNotFound();
            }

            var appInsightsId = application.Fields["ApplicationInsightsId"].Value;
            var applicationId = application.ID.ToString();
            if (!id.Contains("{")) id = "{" + id + "}";

            try
            {
                List<GroupedException> combinedHourlyLogs = GetExceptionsFromApiAndDB(problemIdBase64, innerMostMessageBase64, timespan, applicationId, appInsightsId);
                int minsSince = GetTimeSinceLastHourly(applicationId);

                var viewModel = new SummaryForAIOverview(id, timespan, combinedHourlyLogs);
                viewModel.Application = new Application() { Id = application.ID.ToGuid(), Title = application.Fields["Title"].Value };
                viewModel.Application.ApplicationInsightsId = application.Fields["ApplicationInsightsId"].Value;

                var exceptionsInTimeSpanAsJson = JsonConvert.SerializeObject(viewModel.ExceptionsInTimeSpan);

                return Json(exceptionsInTimeSpanAsJson, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json($"<p>There was a problem generating the AI Overview.</p><p><i>{ex.Message}</i></p>", JsonRequestBehavior.AllowGet);
            }
        }


        /*
        // Used to populate AI Data and Retrieve Summary Of multiple Exceptions Over Time
        // Get exceptions from DB and top up from API <= only this approach gives time data for summary
        // This ensure the very latest data
        */
        public ActionResult GetHealthCheck(string id, string problemIdBase64 = null, string innerMostMessageBase64 = null, string timespan = "30d")
        {
            string error = string.Empty;
            if (id == null)
            {
                return HttpNotFound();
            }
            var application = _master.GetItem(id);
            if (application == null)
            {
                var applications = _master.GetItem(ApplicationsRootID);
                application = applications.GetChildren().Where(x => x.Fields["Title"].Value == "CM").FirstOrDefault();
                if (application == null) application = applications.GetChildren().FirstOrDefault();
                if (application == null) return HttpNotFound();
            }

            var appInsightsId = application.Fields["ApplicationInsightsId"].Value;
            var applicationId = application.ID.ToString();
            if (!id.Contains("{")) id = "{" + id + "}";

            try
            {
                List<GroupedException> combinedHourlyLogs = GetExceptionsFromApiAndDB(problemIdBase64, innerMostMessageBase64, timespan, applicationId, appInsightsId);
                int minsSince = GetTimeSinceLastHourly(applicationId);

                var viewModel = new SummaryForAIOverview(id, timespan, combinedHourlyLogs);
                viewModel.Application = new Application() { Id = application.ID.ToGuid(), Title = application.Fields["Title"].Value };
                viewModel.Application.ApplicationInsightsId = application.Fields["ApplicationInsightsId"].Value;

                var exceptionsInTimeSpanAsJson = JsonConvert.SerializeObject(viewModel.ExceptionsInTimeSpan);


                var prompt = new List<Tuple<string, string>>() {
                        new Tuple<string,string>("System","You are a IT assistant, with in depth knowledge of C# programming language."),
                        new Tuple<string,string>("User",$"Please consider the following JSON enclosed by *** which represents the exceptions found in an application over the last {viewModel.TimeSpan} hours and {minsSince} minutes. Using the provided data, please advise if you see anything unusual in the last 24 hours. For example, large spikes in exceptions, or new or very infrequent exceptions should be classed as unusual. ***{exceptionsInTimeSpanAsJson}***"),
                        new Tuple<string,string>("User",$"Please respond with one a detailed analysis of the new trend."),
                        new Tuple<string,string>("User",$"Please ensure your response uses well formed HTML. Do not use h1 or h2 header tags."),
                        new Tuple<string,string>("User",$"The response should be formatted using HTML tags. The entire response should be valid HTML."),
                        new Tuple<string,string>("User",$"There should be no backticks (i.e. `) in the response."),
                        new Tuple<string,string>("User",$"If nothing unusual is found in the data respond with the message 'APPLICATIONHEALTHY'."),

                    };

                string generatedInsight = _genAIService.Call(prompt, "", "");

                return Json(generatedInsight, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json($"<p>There was a problem generating the AI Overview.</p><p><i>{ex.Message}</i></p>", JsonRequestBehavior.AllowGet);
            }
        }

        /*
        // Used to file if provided, then Retrieve AI Analysis of one Single Exception based on stack trace (encoded in system prompt)
        // Get exceptions from DB and top up from API <= only this approach gives time data for summary
        // This ensure the very latest data
        */
        public ActionResult GetAIOverview(string id, string systemPrompt, string fileName = "")
        {
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                systemPrompt = "You are a helpful assistant that responds completely in valid HTML.";
            }
            else
            {
                systemPrompt = DecodeBase64(systemPrompt);
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var fileContent = getFileContent(fileName);
                systemPrompt += $" +++{fileContent}+++. ";
                systemPrompt += "Please give specific suggestions on how to update the referenced code and refer to the exact lines they need to be applied.";
            }

            var prompt = new List<Tuple<string, string>>() {
                        new Tuple<string,string>("System","You are a IT assistant, with in depth knowledge of C# programming language."),
                        new Tuple<string,string>("User",$"{systemPrompt}"),
                        new Tuple<string,string>("User",$"Please ensure your response uses well formed HTML. Do not use h1 or h2 header tags."),
                        new Tuple<string,string>("User",$"The response should be formatted using HTML tags. The entire response should be valid HTML."),
                        new Tuple<string,string>("User",$"There should be no backticks (i.e. `) in the response.")
                    };

            string generatedInsight = _genAIService.Call(prompt, "", "");

            var response = new
            {
                responseFromAI = generatedInsight,
                base64ResponseFromAI = Base64Encode(generatedInsight),
                file = fileName
            };

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public ActionResult CreatePR(string id, string fileName = "", string previousBase64Response = "", string problemIdBase64 ="")
        {
            var problemId = DecodeBase64(problemIdBase64);
            problemId = RemoveSpecialCharacters(problemId);
            problemId = problemId.Replace(" ", "");

            var previousResponse = DecodeBase64(previousBase64Response);
            var workItemId = CreateWorkItem(previousResponse, problemId);

            var systemPrompt = "";

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var fileContent = getFileContent(fileName);

                systemPrompt += "Please review the file that I have supplied surrounded by +++. Then apply the changes exactly specified in your previous response surrounded by ***.";
                systemPrompt += $" +++{fileContent}+++. ";
                systemPrompt += $" ***{previousResponse}***. ";
            }

            var prompt = new List<Tuple<string, string>>() {
                        new Tuple<string,string>("System","You are a IT assistant, with in depth knowledge of C# programming language."),
                        new Tuple<string,string>("User",$"{systemPrompt}"),
                        //new Tuple<string,string>("User",$"Please ensure your response only contains a base 64 encoded string of the updated files contents. The full file without any truncation should be returned. Please surround the file contents in ***"),
                        new Tuple<string,string>("User",$"Please ensure your response only contains the updated files contents in raw format without escaping. The full amended file without any truncation together with updated lines the should be returned. Please surround the file contents in ***"),

                        new Tuple<string,string>("User",$"There should be no other data supplied in the response. The full updated raw file should be returned without characters escaped")

                    };

            string generatedInsight = _genAIService.Call(prompt, "", "");

            string pattern = @"\*\*\*(.*?)\*\*\*";
            Match match = Regex.Match(generatedInsight, pattern, RegexOptions.Singleline);

            var newResponse = generatedInsight;
            if (match.Success)
            {
                string result = match.Groups[1].Value;

                CreateBranchAndAddCommit($"ai-fix-{problemId}", fileName, result);
            }
            else
            {
                string result = generatedInsight.Replace("*", "");
                CreateBranchAndAddCommit($"ai-fix-{problemId}", fileName, result);
            }
            int prId = _tfsService.CreatePR(problemId, $"ai-fix-{problemId}", "release", workItemId).Result;
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SubmitToBacklog(string id, string fileName = "", string previousBase64Response = "", string problemIdBase64 = "")
        {
            var problemId = DecodeBase64(problemIdBase64);
            problemId = RemoveSpecialCharacters(problemId);
            problemId = problemId.Replace(" ", "");

            var previousResponse = DecodeBase64(previousBase64Response);
            var response = CreateWorkItem(previousResponse, problemId);
            var newResponse = response == 0 ? false : true;
            return Json(newResponse, JsonRequestBehavior.AllowGet);
        }

        private List<GroupedException> GetExceptionsFromApiAndDB(string problemIdBase64, string innerMostMessageBase64, string timespan, string applicationID, string appInsightsId)
        {
            var appFromSQL = _logStore.GetApplication(applicationID);
            int minutesToGetFromApi = 60 - appFromSQL.NextHourly.Subtract(DateTime.Now).Minutes;
            var latestLogsFromApi = new List<GroupedException>();
            if (!string.IsNullOrWhiteSpace(innerMostMessageBase64) || !string.IsNullOrWhiteSpace(problemIdBase64))
            {
                latestLogsFromApi = _appInsightsApi.GetGroupedExceptionsV2(appInsightsId, problemIdBase64, innerMostMessageBase64, $"{minutesToGetFromApi}m").ToList();
            }
            else
            {
                latestLogsFromApi = _appInsightsApi.GetGroupedExceptionsV2(appInsightsId, $"{minutesToGetFromApi}m").ToList();
            }
            foreach (var item in latestLogsFromApi)
            {
                item.DateCreated = appFromSQL.NextHourly;
            }

            var cacheKey = applicationID + problemIdBase64 + AppInsightType.Hourly.ToString() + timespan;
            var hourlyLogsFromDB = _logStore.GetGroupedExceptions(applicationID, problemIdBase64, AppInsightType.Hourly, timespan).ToList();

            var combined = hourlyLogsFromDB.Concat(latestLogsFromApi).ToList();
            return combined;
        }

        private string Strip(string value)
        {
            value = value.Replace("{", "");
            value = value.Replace("}", "");
            return value;
        }
        private ApplicationDetails GetDependencies(Sitecore.Data.Items.Item application)
        {
            var result = new ApplicationDetails();
            var linkedApps = new List<Guid>();

            result.Application = new Application() { Id = application.ID.ToGuid(), Title = application.Fields["Title"].Value };

            result.FirstTier = new List<Application>();
            result.FirstTier.Add(new Application() { Id = application.ID.ToGuid(), Title = application.Fields["Title"].Value });
            linkedApps.Add(application.ID.ToGuid());

            result.SecondTier = new List<Application>();
            result.FirstTier.ForEach(x => _master.GetItem(new ID(x.Id)).GetLinkedItems("dependencies").Where(a => !linkedApps.Contains(a.ID.ToGuid())).ToList().ForEach(a => result.SecondTier.Add(new Application() { Id = a.ID.ToGuid(), Title = a.Fields["Title"].Value })));
            result.SecondTier.ForEach(x => linkedApps.Add(x.Id));

            result.ThirdTier = new List<Application>();
            result.SecondTier.ForEach(x => _master.GetItem(new ID(x.Id)).GetLinkedItems("dependencies").Where(a => !linkedApps.Contains(a.ID.ToGuid())).ToList().ForEach(a => result.ThirdTier.Add(new Application() { Id = a.ID.ToGuid(), Title = a.Fields["Title"].Value })));
            result.ThirdTier.ForEach(x => linkedApps.Add(x.Id));

            result.ForthTier = new List<Application>();
            result.ThirdTier.ForEach(x => _master.GetItem(new ID(x.Id)).GetLinkedItems("dependencies").Where(a => !linkedApps.Contains(a.ID.ToGuid())).ToList().ForEach(a => result.ForthTier.Add(new Application() { Id = a.ID.ToGuid(), Title = a.Fields["Title"].Value })));
            result.ForthTier.ForEach(x => linkedApps.Add(x.Id));

            result.FifthTier = new List<Application>();
            result.ForthTier.ForEach(x => _master.GetItem(new ID(x.Id)).GetLinkedItems("dependencies").Where(a => !linkedApps.Contains(a.ID.ToGuid())).ToList().ForEach(a => result.FifthTier.Add(new Application() { Id = a.ID.ToGuid(), Title = a.Fields["Title"].Value })));
            result.FifthTier.ForEach(x => linkedApps.Add(x.Id));

            return result;
        }
        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string DecodeBase64(string value)
        {
            value = value.Trim();
            string sanitized = Regex.Replace(value, @"[^A-Za-z0-9\+/=]", "");
            int remainder = sanitized.Length % 4;
            if (remainder != 0)
            {
                sanitized = sanitized.PadRight(sanitized.Length + (4 - remainder), '=');
            }
            var valueBytes = System.Convert.FromBase64String(sanitized);
            return Encoding.UTF8.GetString(valueBytes);
        }

        private int GetAjustedTimeSpanInMinutes(string timespan, string applicationId)
        {
            var appFromSQL = _logStore.GetApplication(applicationId);
            int minutesToGetFromApi = 60 - appFromSQL.NextHourly.Subtract(DateTime.Now).Minutes;
            int hours = 1;
            if (timespan.Contains("h"))
            {
                int.TryParse(timespan.Replace("h", ""), out hours);
            }
            else if (timespan.Contains("d"))
            {
                int days = 1;
                int.TryParse(timespan.Replace("d", ""), out days);
                hours = days * 24;
            }
            var timespanInMins = (hours * 60) + minutesToGetFromApi;
            return timespanInMins;
        }
        private int GetTimeSinceLastHourly(string applicationId)
        {
            var appFromSQL = _logStore.GetApplication(applicationId);
            return 60 - appFromSQL.NextHourly.Subtract(DateTime.Now).Minutes;
        }
    }
}