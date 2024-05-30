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
        }
    }
}