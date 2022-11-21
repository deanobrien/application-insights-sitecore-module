using Sitecore.Data.Items;
using System.Linq;
using System;
using System.Collections.Generic;


namespace DeanOBrien.Feature.ApplicationInsights.Tasks
{
    public class AppInsightsImportTask
    {
        public void Execute(Item[] items, Sitecore.Tasks.CommandItem command, Sitecore.Tasks.ScheduleItem schedule)
        {
            Sitecore.Diagnostics.Log.Info("App Insights Import Task: Started", this);
            try
            {
                var apps = new List<Item>();
                if (items.Length == 1)
                {
                    var master = Sitecore.Configuration.Factory.GetDatabase("master");
                    apps = master.GetItem(items[0].ID).GetChildren().Cast<Item>().ToList();
                }
                else {
                    apps = items.ToList();
                }
                var appInsightsImport = new AppInsightsImport();
                appInsightsImport.Run(apps, "AppInsightsImportTask Sitecore Scheduled Task");
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error("Course import error: " + ex.Message, this);
            }
        }
    }
}