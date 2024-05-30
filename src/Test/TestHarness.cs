using System;
using System.Linq;
using System.Text;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;

namespace DeanOBrien.ApplicationInsights.Test
{
    class TestHarness
    {
        private SqlLogStore _sqlStore;
        private AppInsightsApi _api;

        // This is Application (client) ID from App Registration
        private static string entraClientID = "xxx";

        // This is Application (client) Secret from App Registration
        private static string entraClientSecret = "xxx";

        private static string timespan = "2h";

        private static string tenantId = "xxx";

        public TestHarness()
        {
            _sqlStore = new SqlLogStore();
            _api = new AppInsightsApi();
            _api.Initialize(entraClientID, entraClientSecret, tenantId);
        }


        public void Run()
        {
            System.Console.WriteLine("Test Harness for Application Insight Interfaces");

            char option = (char)0;
            while (option != 'q')
            {
                PrintOptions();
                option = System.Console.ReadKey().KeyChar;
                System.Console.WriteLine();

                try
                {
                    switch (option)
                    {
                        case '1':
                            FindApplication();
                            break;

                        case '2':
                            AddApplication();
                            break;

                        case '3':
                            SetNextHourly();
                            break;

                        case '4':
                            SetNextDaily();
                            break;
                        case '5':
                            AddApplicationInsightsLog();
                            break;
                        case '6':
                            PrintApplicationInsightLogs();
                            break;
                        case '7':
                            GetApplicationInsightLogsFromApi();
                            break;
                        case '8':
                            GetApplicationInsightLogsFromApiByProblemId();
                            break;
                        case '9':
                            GetApplicationInsightLogsFromApiByInnerMessage();
                            break;
                        case 'q':
                            return;

                        default:
                            System.Console.WriteLine("Unknown option");
                            break;
                    }
                }
                catch (Exception e)
                {
                    var color = System.Console.ForegroundColor;
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine(e);
                    System.Console.ForegroundColor = color;
                }
            }
        }

        // Case 1
        private Application FindApplication()
        {
            string appId;
            Console.Write("Enter appId: ");
            appId = Console.ReadLine();

            var application = _sqlStore.GetApplication(appId);

            if (application == null)
            {
                Console.WriteLine();
                Console.Write("Application not found");
                Console.WriteLine();
                return null;
            }
            PrintApplication(application);
            return application;
        }
        // Case 2
        private void AddApplication()
        {
            string appId;
            Console.Write("Enter appId: ");
            appId = Console.ReadLine();
            string appTitle;
            Console.Write("Enter appTitle: ");
            appTitle = Console.ReadLine();
            string appInsightsId;
            Console.Write("Enter appInsightsId: ");
            appInsightsId = Console.ReadLine();
            string appInsightskey;
            Console.Write("Enter appInsightskey: ");
            appInsightskey = Console.ReadLine();
            var application = _sqlStore.AddApplication(appId, appTitle, appInsightsId, appInsightskey);

            PrintApplication(application);
        }
        // Case 3
        private void SetNextHourly()
        {
            string appId;
            Console.Write("Enter appId: ");
            appId = Console.ReadLine();

            var application = _sqlStore.GetApplication(appId);
            if (application == null) return;

            string appHours;
            Console.Write("Enter number of hours to add: ");
            appHours = Console.ReadLine();

            DateTime nextHourly = DateTime.Now.AddHours(Convert.ToDouble(appHours));
            _sqlStore.SetNextHourly(appId, nextHourly);

            application = _sqlStore.GetApplication(appId);
            PrintApplication(application);
        }
        // Case 4
        private void SetNextDaily()
        {
            string appId;
            Console.Write("Enter appId: ");
            appId = Console.ReadLine();

            var application = _sqlStore.GetApplication(appId);
            if (application == null) return;

            string appHours;
            Console.Write("Enter number of hours to add: ");
            appHours = Console.ReadLine();

            DateTime nextDaily = DateTime.Now.AddHours(Convert.ToDouble(appHours));
            _sqlStore.SetNextDaily(appId, nextDaily);

            application = _sqlStore.GetApplication(appId);
            PrintApplication(application);
        }
        // Case 5
        private void AddApplicationInsightsLog()
        {
            string appId;
            Console.Write("Enter appId: ");
            appId = Console.ReadLine();
            string appType;
            Console.Write("Enter (1) Daily or (2) Hourly: ");
            appType = Console.ReadLine();
            string problemId;
            Console.Write("Enter problemId: ");
            problemId = Console.ReadLine();
            string count;
            Console.Write("Enter count: ");
            count = Console.ReadLine();

            var application = _sqlStore.GetApplication(appId);
            if (application == null) return;

            _sqlStore.AddGroupedException(appId, (AppInsightType)Convert.ToInt32(appType), problemId, null, null, null, null, null, null,null,null, Convert.ToInt32(count));

            PrintApplication(application);
            PrintApplicationInsightLogs(application);
        }
        // Case 6
        private void PrintApplicationInsightLogs()
        {
            string appId;
            Console.Write("Enter appId: ");
            appId = Console.ReadLine();

            var application = _sqlStore.GetApplication(appId);
            if (application == null) return;

            PrintApplicationInsightLogs(application);
        }
        // Case 7
        private Application GetApplicationInsightLogsFromApi()
        {
            var application = FindApplication();
            if (application == null) return null;

            var logs = _api.GetGroupedExceptionsV2(application.ApplicationInsightsId, "1h");
            foreach (var log in logs)
            {
                _sqlStore.AddGroupedException(application.Id.ToString(), log, AppInsightType.Hourly);
            }

            PrintApplicationInsightLogs(application);
            return application;
        }
        // Case 8
        private Application GetApplicationInsightLogsFromApiByProblemId()
        {
            var application = GetApplicationInsightLogsFromApi();

            string logId;
            Console.Write("Enter logId (to search by ProblemId): ");
            logId = Console.ReadLine();

            var logFromStore = _sqlStore.GetGroupedException(Convert.ToInt32(logId));

            var logs = _api.GetGroupedExceptionsV2(application.ApplicationInsightsId, logFromStore.ProblemIdBase64, null, "24h");
            foreach (var log in logs)
            {
                PrintApplicationInsightLog(log);
            }
            return application;
        }

        // Case 9
        private void GetApplicationInsightLogsFromApiByInnerMessage()
        {
            var application = GetApplicationInsightLogsFromApiByProblemId();

            string logId;
            Console.Write("Enter logId (to search by InnerMessage): ");
            logId = Console.ReadLine();

            var logFromStore = _sqlStore.GetGroupedException(Convert.ToInt32(logId));
            string testInnerMessage = "Multiple controls with the same ID 'FContent70EDF5AF9D454B61859BBC0566241FEF' were found. FindControl requires that controls have unique IDs.";

            var logs = _api.GetGroupedExceptionsV2(application.ApplicationInsightsId, null, Base64Encode(testInnerMessage), "24h");
            foreach (var log in logs)
            {
                PrintApplicationInsightLog(log);
            }
        }
        #region Print to console functions 
        private void PrintApplication(Application application)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Application");
            System.Console.WriteLine($"Id:                      {application.Id}");
            System.Console.WriteLine($"Title:                   {application.Title}");
            System.Console.WriteLine($"NextHourly:              {application.NextHourly}");
            System.Console.WriteLine($"NextDaily:               {application.NextDaily}");
            System.Console.WriteLine($"ApplicationInsightsId:   {application.ApplicationInsightsId}");
            System.Console.WriteLine($"ApplicationInsigtsKey:   {application.ApplicationInsightsKey}");
        }
        private void PrintOptions()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            System.Console.WriteLine("1.    Find Application");
            System.Console.WriteLine("2.    Add Application");
            System.Console.WriteLine("3.    Set Next Hourly For Application");
            System.Console.WriteLine("4.    Set Next Daily For Application");
            System.Console.WriteLine("5.    Add Application Insight Log");
            System.Console.WriteLine("6.    Print Application Insight Logs");
            System.Console.WriteLine("7.    Get Application Insight Logs From Api");
            System.Console.WriteLine("8.   Get Application Insight Logs From Api (more detail by problemId)");
            System.Console.WriteLine("9.   Get Application Insight Logs From Api (more detail by innerMostMessage)");


            System.Console.WriteLine();
            System.Console.WriteLine();
            System.Console.WriteLine("q.    Quit");
            System.Console.WriteLine();
        }
        private void PrintApplicationInsightLogs(Application application)
        {
            var logs = _sqlStore.GetGroupedExceptions(application.Id.ToString(),AppInsightType.Hourly);
            foreach (var log in logs)
            {
                PrintApplicationInsightLog(log);
            }
        }

        private void PrintApplicationInsightLog(GroupedException log)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("App Insight Log");
            System.Console.WriteLine($"Log Id:   {log.Id}");
            System.Console.WriteLine($"ApplicationId:   {log.ApplicationId}");
            System.Console.WriteLine($"AppInsightType:  {log.AppInsightType.ToString()}");
            System.Console.WriteLine($"ProblemId:       {log.ProblemId}");
            System.Console.WriteLine($"ProblemIdBase64: {log.ProblemIdBase64}");
            System.Console.WriteLine($"OuterMessage:       {log.OuterMessage}");
            System.Console.WriteLine($"InnerMostMessage:       {log.InnerMostMessage}");
            System.Console.WriteLine($"Count:           {log.Count}");
            System.Console.WriteLine($"DateCreated:     {log.DateCreated}");
            System.Console.WriteLine();
        }
        #endregion

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string DecodeBase64(string value)
        {
            var valueBytes = System.Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(valueBytes);
        }
    }
}
