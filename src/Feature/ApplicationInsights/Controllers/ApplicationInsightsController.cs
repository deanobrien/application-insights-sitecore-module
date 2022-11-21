using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Sitecore.Data;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights;
using DeanOBrien.Foundation.DataAccess.Models;
using DeanOBrien.Feature.ApplicationInsights.Models;
using DeanOBrien.Feature.ApplicationInsights.Extensions;
using Sitecore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DeanOBrien.Feature.ApplicationInsights.Controllers
{
    public class ApplicationInsightsController : Controller
    {
        private IAppInsightsApi _appInsightsApi;
        private ILogStore _logStore;
        private Database _master;
        private const string ApplicationsRootID = "{3192E2CD-5C42-4EB7-8348-66E887469CD2}";

        public ApplicationInsightsController(IAppInsightsApi appInsightsApi, ILogStore logStore)
        {
            _appInsightsApi= appInsightsApi;
            _logStore = logStore;
            _master = Sitecore.Configuration.Factory.GetDatabase("master");
        }
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
            var viewModel = GetDependencies(application);
            if (problemIdBase64 != null)
            {
                viewModel.ProblemIdBase64 = problemIdBase64;
            }
            if (innerMostMessageBase64 != null)
            {
                viewModel.InnerMessageBase64 = innerMostMessageBase64;
            }
            viewModel.Application.ApplicationInsightsId = application.Fields["ApplicationInsightsId"].Value;
            viewModel.Application.ApplicationInsightsKey = application.Fields["ApplicationInsightsKey"].Value;
            viewModel.TimeSpan = timespan;

            if (!id.Contains("{")) id = "{" + id + "}";
            try
            {
                var dailyLogs = _logStore.GetGroupedExceptions(application.ID.ToString(), problemIdBase64, AppInsightType.Daily, timespan);
                var hourlyLogs = _logStore.GetGroupedExceptions(application.ID.ToString(), problemIdBase64, AppInsightType.Hourly, timespan);
                viewModel.SummaryOfExceptions = new SummaryOfExceptions(id, timespan, dailyLogs, hourlyLogs);
            }
            catch (Exception ex)
            {
                viewModel.ErrorMessage = $"<p>There was a problem getting logs from database, please ensure you have installed ApplicationInsights DacPac and added the <b>ApplicationInsights</b> connection string.</p><p><i>{ex.Message}</i></p>";
            }
            return View("~/sitecore/shell/client/Applications/ApplicationInsights/Index.cshtml", viewModel);
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

        public ActionResult GroupedExceptions(string id, string problemIdBase64 =null, string innerMostMessageBase64=null, string timespan = "2h")
        {
            if (id == null) return Json(new { ErrorMessage = "Application id missing" }, JsonRequestBehavior.AllowGet);
            
            var application = _master.GetItem(id);

            if (application == null) return Json(new { ErrorMessage = "Application not found" }, JsonRequestBehavior.AllowGet);            
            else if (application.Fields["ApplicationInsightsId"] == null
                || string.IsNullOrWhiteSpace(application.Fields["ApplicationInsightsId"].Value)
                || application.Fields["ApplicationInsightsKey"] == null
                || string.IsNullOrWhiteSpace(application.Fields["ApplicationInsightsKey"].Value)) return Json(new { ErrorMessage = "ApplicationInsightsId or ApplicationInsightsKey not set" }, JsonRequestBehavior.AllowGet);
            
            var appInsightsId = application.Fields["ApplicationInsightsId"].Value;
            var appInsightsKey = application.Fields["ApplicationInsightsKey"].Value;
            var result = new List<GroupedException>();

            if (!string.IsNullOrWhiteSpace(innerMostMessageBase64) || !string.IsNullOrWhiteSpace(problemIdBase64))
            {
                result = _appInsightsApi.GetGroupedExceptions(appInsightsId, appInsightsKey, problemIdBase64, innerMostMessageBase64, timespan);
            }
            else
            {
                result = _appInsightsApi.GetGroupedExceptions(appInsightsId, appInsightsKey, timespan);
            }
            foreach (var item in result)
            {
                item.ApplicationId = id;
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        private string Strip(string value) {
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
    }
}