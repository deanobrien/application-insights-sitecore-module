using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights
{
    public class SqlLogStore : ILogStore
    {
        public void AddTriggeredAlert(string applicationId, string alertId, string title, DateTime dateTriggered)
        {
            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationInsights"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("dbo.InsertTriggeredAlert", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var parameterApplicationId = new SqlParameter("@ApplicationId", SqlDbType.VarChar, 50);
                        parameterApplicationId.Value = applicationId;
                        cmd.Parameters.Add(parameterApplicationId);

                        var parameterAlertId = new SqlParameter("@AlertId", SqlDbType.VarChar, 50);
                        parameterAlertId.Value = alertId;
                        cmd.Parameters.Add(parameterAlertId);

                        var parameterTitle = new SqlParameter("@Title", SqlDbType.VarChar, 50);
                        parameterTitle.Value = title;
                        cmd.Parameters.Add(parameterTitle);

                        string format = "yyyy-MM-dd HH:mm:ss";    // modify the format depending upon input required in the column in database

                        var parameterDateTriggered = new SqlParameter("@DateTriggered", SqlDbType.DateTime);
                        parameterDateTriggered.Value = dateTriggered.ToString(format);
                        cmd.Parameters.Add(parameterDateTriggered);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public List<TriggeredAlert> GetTriggeredAlerts(string applicationId, DateTime dateFrom)
        {
            var triggeredAlerts = new List<TriggeredAlert>();

            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationInsights"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("dbo.FindTriggeredAlertsById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var parameterApplicationId = new SqlParameter("@ApplicationId", SqlDbType.VarChar, 50);
                        parameterApplicationId.Value = applicationId;
                        cmd.Parameters.Add(parameterApplicationId);

                        string format = "yyyy-MM-dd HH:mm:ss";

                        var parameterDateFrom = new SqlParameter("@DateFrom", SqlDbType.DateTime);
                        parameterDateFrom.Value = dateFrom.ToString(format);
                        cmd.Parameters.Add(parameterDateFrom);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    var triggeredAlert = new TriggeredAlert();
                                    triggeredAlert.Id = reader.GetInt32(0);
                                    triggeredAlert.ApplicationId = reader.GetString(1);
                                    triggeredAlert.AlertId = reader.GetString(2);
                                    triggeredAlert.Title = reader.GetString(3);
                                    triggeredAlert.DateTriggered = reader.GetDateTime(4);
                                    triggeredAlert.DayTriggered = triggeredAlert.DateTriggered.Day;
                                    triggeredAlert.MonthTriggered = triggeredAlert.DateTriggered.ToString("MMMM");
                                    triggeredAlerts.Add(triggeredAlert);
                                }
                            }
                        }
                        conn.Close();
                    }
                }
                return triggeredAlerts;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public Application GetApplication(string applicationId)
        {
            var application = new Application();

            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationInsights"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("dbo.FindApplicationById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var parameterApplicationId = new SqlParameter("@ApplicationId", SqlDbType.VarChar, 50);
                        parameterApplicationId.Value = applicationId;
                        cmd.Parameters.Add(parameterApplicationId);


                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    application.Id = new Guid(reader.GetString(0));
                                    application.Title = reader.GetString(1);
                                    application.NextHourly = reader.GetDateTime(2);
                                    application.NextDaily = reader.GetDateTime(3);
                                    application.ApplicationInsightsId = reader.GetString(4);
                                    application.ApplicationInsightsKey = reader.GetString(5);
                                }
                            }
                        }
                        conn.Close();
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (string.IsNullOrWhiteSpace(application.ApplicationInsightsId)) return null;
            return application;
        }
        public Application AddApplication(string applicationId, string title, string applicationInsightsId, string applicationInsightsKey)
        {
            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationInsights"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("dbo.InsertApplication", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var parameterApplicationId = new SqlParameter("@ApplicationId", SqlDbType.VarChar, 50);
                        parameterApplicationId.Value = applicationId;
                        cmd.Parameters.Add(parameterApplicationId);

                        var parameterTitle = new SqlParameter("@Title", SqlDbType.VarChar, 50);
                        parameterTitle.Value = title;
                        cmd.Parameters.Add(parameterTitle);

                        string format = "yyyy-MM-dd HH:mm:ss";    // modify the format depending upon input required in the column in database

                        var parameterNextHourly = new SqlParameter("@NextHourly", SqlDbType.DateTime);
                        parameterNextHourly.Value = DateTime.Now.ToString(format);
                        cmd.Parameters.Add(parameterNextHourly);

                        var parameterNextDaily = new SqlParameter("@NextDaily", SqlDbType.DateTime);
                        parameterNextDaily.Value = DateTime.Now.ToString(format);
                        cmd.Parameters.Add(parameterNextDaily);

                        var parameterInsightsId = new SqlParameter("@InsightsId", SqlDbType.VarChar, 50);
                        parameterInsightsId.Value = applicationInsightsId;
                        cmd.Parameters.Add(parameterInsightsId);

                        var parameterInsightsKey = new SqlParameter("@InsightsKey", SqlDbType.VarChar, 50);
                        parameterInsightsKey.Value = applicationInsightsKey;
                        cmd.Parameters.Add(parameterInsightsKey);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return GetApplication(applicationId);
        }

        public void SetNextDaily(string applicationId, DateTime nextDaily)
        {
            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationInsights"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("dbo.SetnextDaily", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var parameterApplicationId = new SqlParameter("@ApplicationId", SqlDbType.VarChar, 50);
                        parameterApplicationId.Value = applicationId;
                        cmd.Parameters.Add(parameterApplicationId);

                        string format = "yyyy-MM-dd HH:mm:ss";    // modify the format depending upon input required in the column in database

                        var parameterNextDaily = new SqlParameter("@NextDaily", SqlDbType.VarChar, 50);
                        parameterNextDaily.Value = nextDaily.ToString(format);
                        cmd.Parameters.Add(parameterNextDaily);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SetNextHourly(string applicationId, DateTime nextHourly)
        {
            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationInsights"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("dbo.SetNextHourly", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var parameterApplicationId = new SqlParameter("@ApplicationId", SqlDbType.VarChar, 50);
                        parameterApplicationId.Value = applicationId;
                        cmd.Parameters.Add(parameterApplicationId);

                        string format = "yyyy-MM-dd HH:mm:ss";    // modify the format depending upon input required in the column in database

                        var parameterNextHourly = new SqlParameter("@NextHourly", SqlDbType.VarChar, 50);
                        parameterNextHourly.Value = nextHourly.ToString(format);
                        cmd.Parameters.Add(parameterNextHourly);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void AddGroupedException(string applicationId, GroupedException exception, AppInsightType applicationType)
        {
            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationInsights"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("dbo.InsertAppInsightLog", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var parameterApplicationId = new SqlParameter("@ApplicationId", SqlDbType.VarChar, 50);
                        parameterApplicationId.Value = applicationId;
                        cmd.Parameters.Add(parameterApplicationId);

                        var parameterAppInsightType = new SqlParameter("@AppInsightType", SqlDbType.Int, 10);
                        parameterAppInsightType.Value = applicationType;
                        cmd.Parameters.Add(parameterAppInsightType);

                        string format = "yyyy-MM-dd HH:mm:ss";    // modify the format depending upon input required in the column in database

                        var parameterDateCreated = new SqlParameter("@DateCreated", SqlDbType.DateTime);
                        parameterDateCreated.Value = DateTime.Now.ToString(format);
                        cmd.Parameters.Add(parameterDateCreated);

                        var parameterProblemId = new SqlParameter("@ProblemId", SqlDbType.VarChar, 8000);
                        parameterProblemId.Value = exception.ProblemId;
                        cmd.Parameters.Add(parameterProblemId);

                        var parameterProblemIdBase64 = new SqlParameter("@ProblemIdBase64", SqlDbType.VarChar);
                        parameterProblemIdBase64.Value = exception.ProblemIdBase64;
                        cmd.Parameters.Add(parameterProblemIdBase64);

                        var parameterOuterType = new SqlParameter("@OuterType", SqlDbType.VarChar, 8000);
                        parameterOuterType.Value = exception.OuterType;
                        cmd.Parameters.Add(parameterOuterType);

                        var parameterType = new SqlParameter("@Type", SqlDbType.VarChar, 8000);
                        parameterType.Value = exception.Type;
                        cmd.Parameters.Add(parameterType);

                        var parameterInnermostType = new SqlParameter("@InnermostType", SqlDbType.VarChar, 8000);
                        parameterInnermostType.Value = exception.InnerMostType;
                        cmd.Parameters.Add(parameterInnermostType);

                        var parameterOuterAssembly = new SqlParameter("@OuterAssembly", SqlDbType.VarChar, 8000);
                        parameterOuterAssembly.Value = exception.OuterAssembly;
                        cmd.Parameters.Add(parameterOuterAssembly);

                        var parameterAssembly = new SqlParameter("@Assembly", SqlDbType.VarChar, 8000);
                        parameterAssembly.Value = exception.Assembly;
                        cmd.Parameters.Add(parameterAssembly);

                        var parameterOuterMethod = new SqlParameter("@OuterMethod", SqlDbType.VarChar, 8000);
                        parameterOuterMethod.Value = exception.OuterMethod;
                        cmd.Parameters.Add(parameterOuterMethod);

                        var parameterMethod = new SqlParameter("@Method", SqlDbType.VarChar, 8000);
                        parameterMethod.Value = exception.Method;
                        cmd.Parameters.Add(parameterMethod);

                        var parameterCount = new SqlParameter("@Count", SqlDbType.Int, 10);
                        parameterCount.Value = exception.Count;
                        cmd.Parameters.Add(parameterCount);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void AddGroupedException(string applicationId, AppInsightType applicationType, string problemId, string problemIdBase64, string outerType, string type, string innermostType, string outerAssembly, string assembly, string outerMethod, string method, int count)
        {
            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationInsights"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("dbo.InsertAppInsightLog", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var parameterApplicationId = new SqlParameter("@ApplicationId", SqlDbType.VarChar, 50);
                        parameterApplicationId.Value = applicationId;
                        cmd.Parameters.Add(parameterApplicationId);

                        var parameterAppInsightType = new SqlParameter("@AppInsightType", SqlDbType.Int, 10);
                        parameterAppInsightType.Value = applicationType;
                        cmd.Parameters.Add(parameterAppInsightType);

                        string format = "yyyy-MM-dd HH:mm:ss";    // modify the format depending upon input required in the column in database

                        var parameterDateCreated = new SqlParameter("@DateCreated", SqlDbType.VarChar, 50);
                        parameterDateCreated.Value = DateTime.Now.ToString(format);
                        cmd.Parameters.Add(parameterDateCreated);

                        var parameterProblemId = new SqlParameter("@ProblemId", SqlDbType.VarChar, 8000);
                        parameterProblemId.Value = problemId;
                        cmd.Parameters.Add(parameterProblemId);

                        var parameterProblemIdBase64 = new SqlParameter("@ProblemIdBase64", SqlDbType.VarChar);
                        parameterProblemIdBase64.Value = problemIdBase64;
                        cmd.Parameters.Add(parameterProblemIdBase64);

                        var parameterOuterType = new SqlParameter("@OuterType", SqlDbType.VarChar, 8000);
                        parameterOuterType.Value = outerType;
                        cmd.Parameters.Add(parameterOuterType);

                        var parameterType = new SqlParameter("@Type", SqlDbType.VarChar, 8000);
                        parameterType.Value = type;
                        cmd.Parameters.Add(parameterType);

                        var parameterInnermostType = new SqlParameter("@InnermostType", SqlDbType.VarChar, 8000);
                        parameterInnermostType.Value = innermostType;
                        cmd.Parameters.Add(parameterInnermostType);

                        var parameterOuterAssembly = new SqlParameter("@OuterAssembly", SqlDbType.VarChar, 8000);
                        parameterOuterAssembly.Value = outerAssembly;
                        cmd.Parameters.Add(parameterOuterAssembly);

                        var parameterAssembly = new SqlParameter("@Assembly", SqlDbType.VarChar, 8000);
                        parameterAssembly.Value = assembly;
                        cmd.Parameters.Add(parameterAssembly);

                        var parameterOuterMethod = new SqlParameter("@OuterMethod", SqlDbType.VarChar, 8000);
                        parameterOuterMethod.Value = outerMethod;
                        cmd.Parameters.Add(parameterOuterMethod);

                        var parameterMethod = new SqlParameter("@Method", SqlDbType.VarChar, 8000);
                        parameterMethod.Value = method;
                        cmd.Parameters.Add(parameterMethod);

                        var parameterCount = new SqlParameter("@Count", SqlDbType.Int, 10);
                        parameterCount.Value = count;
                        cmd.Parameters.Add(parameterCount);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<GroupedException> GetGroupedExceptions(string applicationId, AppInsightType appInsightType, string timespan="7d")
        {
            int hours = ConvertTimespanToHours(timespan);

            var result = new List<GroupedException>();

            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationInsights"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("dbo.FindAppInsightLogsById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var parameterAppInsightType = new SqlParameter("@AppInsightType", SqlDbType.Int, 2);
                        parameterAppInsightType.Value = (int)appInsightType;
                        cmd.Parameters.Add(parameterAppInsightType);

                        var parameterApplicationId = new SqlParameter("@ApplicationId", SqlDbType.VarChar, 50);
                        parameterApplicationId.Value = applicationId;
                        cmd.Parameters.Add(parameterApplicationId);

                        string format = "yyyy-MM-dd HH:mm:ss";    

                        var parameterDateFrom = new SqlParameter("@DateFrom", SqlDbType.DateTime);
                        parameterDateFrom.Value = DateTime.Now.AddHours(-1 * hours).ToString(format);
                        cmd.Parameters.Add(parameterDateFrom);

                        ExecuteGroupedExceptionStoredProcedure(result, conn, cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }


        public List<GroupedException> GetGroupedExceptions(string applicationId, string problemIdBase64 = null, AppInsightType appInsightType = AppInsightType.Daily, string timespan="7d")
        {



            int hours = ConvertTimespanToHours(timespan);

            if (string.IsNullOrWhiteSpace(problemIdBase64)) return GetGroupedExceptions(applicationId, appInsightType, timespan);

            var result = new List<GroupedException>();

            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationInsights"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("dbo.FindAppInsightLogsByIdAndProblemId", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var parameterAppInsightType = new SqlParameter("@AppInsightType", SqlDbType.Int, 2);
                        parameterAppInsightType.Value = (int)appInsightType;
                        cmd.Parameters.Add(parameterAppInsightType);

                        var parameterApplicationId = new SqlParameter("@ApplicationId", SqlDbType.VarChar, 50);
                        parameterApplicationId.Value = applicationId;
                        cmd.Parameters.Add(parameterApplicationId);

                        var parameterProblemIdBase64 = new SqlParameter("@ProblemIdBase64", SqlDbType.VarChar, 200);
                        parameterProblemIdBase64.Value = problemIdBase64;
                        cmd.Parameters.Add(parameterProblemIdBase64);

                        string format = "yyyy-MM-dd HH:mm:ss";

                        var parameterDateFrom = new SqlParameter("@DateFrom", SqlDbType.DateTime);
                        parameterDateFrom.Value = DateTime.Now.AddHours(-1 * hours).ToString(format);
                        cmd.Parameters.Add(parameterDateFrom);

                        ExecuteGroupedExceptionStoredProcedure(result, conn, cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        private static int ConvertTimespanToHours(string timespan)
        {
            int timespanInHours = 168; // default to 7days
            if (timespan.Contains("d"))
            {
                int days;
                timespan = timespan.Replace("d", "");
                if (int.TryParse(timespan, out days)) timespanInHours = days * 24;
            }
            else if (timespan.Contains("h"))
            {
                int hours;
                timespan = timespan.Replace("h", "");
                if (int.TryParse(timespan, out hours)) timespanInHours = hours;
            }

            return timespanInHours;
        }

        public GroupedException GetGroupedException(int logId)
        {
            var result = new List<GroupedException>();

            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationInsights"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("dbo.FindAppInsightLogById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var parameterLogId = new SqlParameter("@LogId", SqlDbType.Int, 15);
                        parameterLogId.Value = logId;
                        cmd.Parameters.Add(parameterLogId);

                        ExecuteGroupedExceptionStoredProcedure(result, conn, cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result.FirstOrDefault();
        }


        private static void ExecuteGroupedExceptionStoredProcedure(List<GroupedException> result, SqlConnection conn, SqlCommand cmd)
        {
            conn.Open();
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        GroupedException exception = ConvertToGroupedException(reader);
                        result.Add(exception);
                    }
                }
            }
            conn.Close();
        }

        private static GroupedException ConvertToGroupedException(SqlDataReader reader)
        {
            var exception = new GroupedException();
            exception.Id = reader.GetInt32(0);
            exception.ApplicationId = reader.GetString(1);
            exception.AppInsightType = (AppInsightType)reader.GetInt32(2);
            exception.ProblemId = reader.GetString(3);
            exception.ProblemIdBase64 = reader.GetString(4);
            exception.OuterType = reader.GetString(5);
            exception.Type = reader.GetString(6);
            exception.InnerMostType = reader.GetString(7);
            exception.OuterAssembly = reader.GetString(8);
            exception.Assembly = reader.GetString(9);
            exception.OuterMethod = reader.GetString(10);
            exception.Method = reader.GetString(11);
            exception.Count = reader.GetInt32(12);
            exception.DateCreated = reader.GetDateTime(13);
            return exception;
        }
    }
}
