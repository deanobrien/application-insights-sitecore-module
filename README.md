# Application Insights for Sitecore Module

![enter image description here](https://deanobrien.uk/wp-content/uploads/2022/11/ApplicationInsights.png)

Some of the key benefits of this module are as follows:

 - Quickly view the health of ALL Sitecore / Xconnect instances on one screen.

 - Show a list of all exceptions happening on a given instance and their frequency

 - Group exceptions by ProblemId and then by InnerMostMessage

 - Track exceptions over time to see when they began or at what times during the day they occur

 - View logs from as far back as the moment you start tracking them
 
 - Extend to track any application using application insights

 - Entra Authentication (more info Switch to Entra Auth)

 - Add the following Alerts (more info Alerting added to App Insights module)

   - Exception Contains String

   - Exception Spike

 - Schedule Task Inactivity

 - Cron Task Inactivity

 - Custom Event

 - Webpage Down

 - Service Bus Queue Exceeds

 - AI Alerts

 - Azure Devops / TFS Integration

 - AI Summary of all Exceptions over given period

 - Ask AI any question about all exceptions in given period

 - AI Analysis and recommendation based Exception and Custom code (from your repo)

 - Generate Work Items or PRs based on Recommendations

 - AI Alerting - this compares recent exceptions against last X days and alerts if it finds anything 'out of ordinary' (all prompts are configurable)

 
 --------------------------------
 
 Installation Instructions
 
 1) Download zip file and unpack
 2) Install new ApplicationInsights database using DACPAC
 3) Install Sitecore package
 4) Go to [Azure Portal and configure Microsoft Entra Authentication](https://deanobrien.uk/update-application-insights-module-to-switch-to-use-entra-authentication/)
 6) Go to Dashboard => Application Insights
 
 
> Detailed guide can be found here: [DeanOBrien: Application Insights Module for Sitecore](https://deanobrien.uk/application-insights-for-sitecore-module/).
