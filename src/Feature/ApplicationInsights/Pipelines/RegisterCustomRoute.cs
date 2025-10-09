using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI.WebControls;
using Sitecore.Pipelines;

namespace DeanOBrien.Feature.ApplicationInsights.Pipelines
{
    public class RegisterCustomRoute
    {
        public virtual void Process(PipelineArgs args)
        {
            Register();
        }

        public static void Register()
        {
            RouteTable.Routes.MapRoute("ApplicationInsightsDependencies", "sitecore/shell/sitecore/client/applications/applicationinsights/dependencies", new { controller = "ApplicationInsights", action = "Dependencies" });
            RouteTable.Routes.MapRoute("ApplicationInsights", "sitecore/shell/sitecore/client/applications/applicationinsights/{id}", new { controller = "ApplicationInsights", action = "Index" });
            RouteTable.Routes.MapRoute("ApplicationInsightsGroupedExceptions", "sitecore/shell/sitecore/client/applications/applicationinsights/groupedExceptions/{id}", new { controller = "ApplicationInsights", action = "GroupedExceptions" });
            RouteTable.Routes.MapRoute("GetAlerts", "sitecore/shell/sitecore/client/applications/applicationinsights/GetAlerts/{id}", new { controller = "ApplicationInsights", action = "GetAlerts" });
            RouteTable.Routes.MapRoute("GetAISummary", "sitecore/shell/sitecore/client/applications/applicationinsights/GetAISummary/{id}", new { controller = "ApplicationInsights", action = "GetAISummary" });
            RouteTable.Routes.MapRoute("GetAIOverview2", "sitecore/shell/sitecore/client/applications/applicationinsights/GetAIOverview/{id}", new { controller = "ApplicationInsights", action = "GetAIOverview" });
            RouteTable.Routes.MapRoute("GetAISummaryData", "sitecore/shell/sitecore/client/applications/applicationinsights/GetAISummaryData/{id}", new { controller = "ApplicationInsights", action = "GetAISummaryData" });
            RouteTable.Routes.MapRoute("GetHealthCheck", "sitecore/shell/sitecore/client/applications/applicationinsights/GetHealthCheck/{id}", new { controller = "ApplicationInsights", action = "GetHealthCheck" });
            RouteTable.Routes.MapRoute("SubmitToBacklog", "sitecore/shell/sitecore/client/applications/applicationinsights/SubmitToBacklog/{id}", new { controller = "ApplicationInsights", action = "SubmitToBacklog" });
            RouteTable.Routes.MapRoute("CreatePR", "sitecore/shell/sitecore/client/applications/applicationinsights/CreatePR/{id}", new { controller = "ApplicationInsights", action = "CreatePR" });



        }

    }
}