using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;
using Newtonsoft.Json;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights
{
    public class AppInsightsApi : IAppInsightsApi
    {
        private static string EntraClientID { get; set; }
        private static string EntraClientSecret { get; set; }
        private static string TenantID { get; set; }
        public static BearerResponse BearerToken { get; set; }
        public static DateTimeOffset TokenExpiryDate { get; set; }



        public void Initialize(string entraClientID, string entraClientSecret, string tenantId)
        {
            EntraClientID = entraClientID;
            EntraClientSecret= entraClientSecret;
            TenantID = tenantId;
        }
        private static string GetBearerToken()
        {
            if (BearerToken != null)
            {
                if (TokenExpiryDate > DateTime.Now) return BearerToken.access_token;
            }

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://login.microsoftonline.com/");
            client.DefaultRequestHeaders.Accept.Clear();

            var nvc = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", EntraClientID),
                new KeyValuePair<string, string>("resource", "https://api.applicationinsights.io"),
                new KeyValuePair<string, string>("client_secret", EntraClientSecret)
            };
            var request = new HttpRequestMessage(HttpMethod.Post, TenantID + "/oauth2/token") { Content = new FormUrlEncodedContent(nvc) };

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.SendAsync(request).Result;
            if (response.IsSuccessStatusCode)
            {
                BearerToken = JsonConvert.DeserializeObject<BearerResponse>(response.Content.ReadAsStringAsync().Result);
                TokenExpiryDate = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt64(60));
                return BearerToken.access_token;
            }
            return null;
        }

        [OutputCache(Duration = 240, VaryByParam = "applicationInsightsId,applicationInsightsKey,timespan")]
        public List<GroupedException> GetGroupedExceptions(string applicationInsightsId, string applicationInsightsKey, string timespan)
        {
            if (string.IsNullOrWhiteSpace(applicationInsightsId) && string.IsNullOrWhiteSpace(applicationInsightsKey)) return null;
            string query = BuildBasicQuery(timespan);
            var apiResponse = CallInsightsAPI(applicationInsightsId, applicationInsightsKey, query);
            return DeserialiseResponse(apiResponse, false);
        }
        [OutputCache(Duration = 240, VaryByParam = "applicationInsightsId,applicationInsightsKey,problemIdBase64,innerMostMessageBase64,timespan")]
        public List<GroupedException> GetGroupedExceptions(string applicationInsightsId, string applicationInsightsKey, string problemIdBase64, string innerMostMessageBase64, string timespan)
        {
            if (string.IsNullOrWhiteSpace(applicationInsightsId) && string.IsNullOrWhiteSpace(applicationInsightsKey)) return null;

            // Hack => see Issue #1
            if (!string.IsNullOrWhiteSpace(problemIdBase64) && !string.IsNullOrWhiteSpace(innerMostMessageBase64)) return GetGroupedExceptionsV2(applicationInsightsId, problemIdBase64, null, timespan).Where(x => x.InnerMostMessageBase64 == innerMostMessageBase64).ToList();

            string query = BuildDetailedQuery(problemIdBase64, innerMostMessageBase64, timespan);
            var apiResponse = CallInsightsAPI(applicationInsightsId, applicationInsightsKey, query);
            return DeserialiseResponse(apiResponse, true);
        }

        [OutputCache(Duration = 240, VaryByParam = "applicationInsightsId,timespan")]
        public List<GroupedException> GetGroupedExceptionsV2(string applicationInsightsId, string timespan)
        {
            Log.Info("GetGroupedExceptionsV2() start", this);
            if (string.IsNullOrWhiteSpace(applicationInsightsId)) return null;
            string query = BuildBasicQuery(timespan);
            var apiResponse = CallInsightsAPIV2(applicationInsightsId, query);
            Log.Info("GetGroupedExceptionsV2() end", this);
            return DeserialiseResponse(apiResponse, false);
        }
        [OutputCache(Duration = 240, VaryByParam = "applicationInsightsId,problemIdBase64,innerMostMessageBase64,timespan")]
        public List<GroupedException> GetGroupedExceptionsV2(string applicationInsightsId, string problemIdBase64, string innerMostMessageBase64, string timespan)
        {
            if (string.IsNullOrWhiteSpace(applicationInsightsId)) return null;

            // Hack => see Issue #1
            if (!string.IsNullOrWhiteSpace(problemIdBase64) && !string.IsNullOrWhiteSpace(innerMostMessageBase64)) return GetGroupedExceptionsV2(applicationInsightsId, problemIdBase64, null, timespan).Where(x => x.InnerMostMessageBase64 == innerMostMessageBase64).ToList();

            string query = BuildDetailedQuery(problemIdBase64, innerMostMessageBase64, timespan);
            var apiResponse = CallInsightsAPIV2(applicationInsightsId, query);
            return DeserialiseResponse(apiResponse, true);
        }

        //[OutputCache(Duration = 240, VaryByParam = "applicationInsightsId,problemIdBase64,innerMostMessageBase64,timespan")]
        public List<SingleException> GetSingleException(string applicationInsightsId, string problemIdBase64, string innerMostMessageBase64, string timespan)
        {
            if (string.IsNullOrWhiteSpace(applicationInsightsId)) return null;


            string query = BuildDetailedQuery2(problemIdBase64, innerMostMessageBase64, timespan);
            Log.Info($"AppInsightsApi: { query }", this);
            var apiResponse = CallInsightsAPIV2(applicationInsightsId, query);
            var x =  DeserialiseResponse2(apiResponse, true);
            return x;
        }

        [OutputCache(Duration = 240, VaryByParam = "applicationInsightsId,timespan")]
        public List<CustomEventSummary> GetCustomEventsV2(string applicationInsightsId, string customEvent, string timespan)
        {
            if (string.IsNullOrWhiteSpace(timespan)) return new List<CustomEventSummary>();
            if (string.IsNullOrWhiteSpace(customEvent)) return new List<CustomEventSummary>();

            Log.Info("GetCustomEventsV2() start", this);
            if (string.IsNullOrWhiteSpace(applicationInsightsId)) return null;
            string query = BuildCustomEventsQuery(customEvent, timespan);
            var apiResponse = CallInsightsAPIV2(applicationInsightsId, query);
            Log.Info("GetCustomEventsV2() end", this);
            return DeserialiseCustomEventResponse(apiResponse);
        }
        private static string BuildCustomEventsQuery(string customEvent, string timespan)
        {
            string newQuery = $"customEvents | where name contains '{customEvent}' | where timestamp > ago({timespan}) | summarize number = count(name) by name";
            string query = $"/query?query={HttpUtility.UrlPathEncode(newQuery)}";
            return query;
        }
        private static string BuildBasicQuery(string timespan)
        {
            string newQuery = $"exceptions | where timestamp > ago({timespan}) | summarize number = count(problemId) by problemId, outerType, type, innermostType, outerAssembly, assembly, outerMethod, method | order by number";
            string query = $"/query?query={HttpUtility.UrlPathEncode(newQuery)}";
            return query;
        }
        private static string BuildDetailedQuery(string problemIdBase64, string innerMostMessageBase64, string timespan)
        {
            string newQuery = string.Empty;

            // Issue #1 => on few occasions when adding innermostMessage it fails the api call (i.e. when /" in message) => workaround: calling without then filtering on innermostMessage
            // if ((!string.IsNullOrWhiteSpace(problemIdBase64)) && (!string.IsNullOrWhiteSpace(innerMostMessageBase64))){ newQuery = $"exceptions | where timestamp > ago({timespan}) | where problemId == '{DecodeBase64(problemIdBase64)}' and innermostMessage == '{DecodeBase64(innerMostMessageBase64)}' | summarize number = count(problemId) by problemId, outerType, type, innermostType, outerAssembly, assembly, outerMethod, method, outerMessage, innermostMessage | order by number";} else

            if (!string.IsNullOrWhiteSpace(problemIdBase64))
            {
                newQuery = $"exceptions | where timestamp > ago({timespan}) | where problemId == '{DecodeBase64(problemIdBase64)}' | summarize number = count(problemId) by problemId, outerType, type, innermostType, outerAssembly, assembly, outerMethod, method, outerMessage, innermostMessage | order by number";
            }
            else if (!string.IsNullOrWhiteSpace(innerMostMessageBase64))
            {
                newQuery = $"exceptions | where timestamp > ago({timespan}) | where innermostMessage == '{DecodeBase64(innerMostMessageBase64)}' | summarize number = count(problemId) by problemId, outerType, type, innermostType, outerAssembly, assembly, outerMethod, method, outerMessage, innermostMessage | order by number";
            }
            return $"/query?query={HttpUtility.UrlPathEncode(newQuery)}";
        }
        private static string BuildDetailedQuery2(string problemIdBase64, string innerMostMessageBase64, string timespan)
        {
            string newQuery = string.Empty;

            // Issue #1 => on few occasions when adding innermostMessage it fails the api call (i.e. when /" in message) => workaround: calling without then filtering on innermostMessage
            // if ((!string.IsNullOrWhiteSpace(problemIdBase64)) && (!string.IsNullOrWhiteSpace(innerMostMessageBase64))){ newQuery = $"exceptions | where timestamp > ago({timespan}) | where problemId == '{DecodeBase64(problemIdBase64)}' and innermostMessage == '{DecodeBase64(innerMostMessageBase64)}' | summarize number = count(problemId) by problemId, outerType, type, innermostType, outerAssembly, assembly, outerMethod, method, outerMessage, innermostMessage | order by number";} else

            if (!string.IsNullOrWhiteSpace(problemIdBase64) && !string.IsNullOrWhiteSpace(innerMostMessageBase64)) 
            {
                newQuery = $"exceptions | where timestamp > ago({timespan}) | where problemId == '{DecodeBase64(problemIdBase64)}' | where innermostMessage == '{DecodeBase64(innerMostMessageBase64)}'";
            }
            else if (!string.IsNullOrWhiteSpace(problemIdBase64))
            {
                newQuery = $"exceptions | where timestamp > ago({timespan}) | where problemId == '{DecodeBase64(problemIdBase64)}'";
            }
            else if (!string.IsNullOrWhiteSpace(innerMostMessageBase64))
            {
                newQuery = $"exceptions | where timestamp > ago({timespan}) | where innermostMessage == '{DecodeBase64(innerMostMessageBase64)}'";
            }
            return $"/query?query={HttpUtility.UrlPathEncode(newQuery)}";
        }
        private static List<GroupedException> DeserialiseResponse(ApiResponse apiResponse, bool detailed = false)
        {
            var result = new List<GroupedException>();
            if (apiResponse.tables.Any() && apiResponse.tables[0] != null && apiResponse.tables[0].rows != null && apiResponse.tables[0].rows.Count() > 0)
            {
                apiResponse.tables[0].rows.ToList().ForEach(x => result.Add(new GroupedException(JsonConvert.DeserializeObject<object[]>(x.ToString()), detailed)));
            }
            return result;
        }
        private static List<SingleException> DeserialiseResponse2(ApiResponse apiResponse, bool detailed = false)
        {
            var result = new List<SingleException>();
            if (apiResponse.tables.Any() && apiResponse.tables[0] != null && apiResponse.tables[0].rows != null && apiResponse.tables[0].rows.Count() > 0)
            {
                apiResponse.tables[0].rows.ToList().ForEach(x => result.Add(new SingleException(JsonConvert.DeserializeObject<object[]>(x.ToString()), detailed)));
            }
            return result;
        }
        private static List<CustomEventSummary> DeserialiseCustomEventResponse(ApiResponse apiResponse)
        {
            var result = new List<CustomEventSummary>();
            if (apiResponse.tables.Any() && apiResponse.tables[0] != null && apiResponse.tables[0].rows != null && apiResponse.tables[0].rows.Count() > 0)
            {
                apiResponse.tables[0].rows.ToList().ForEach(x => result.Add(new CustomEventSummary(JsonConvert.DeserializeObject<object[]>(x.ToString()))));
            }
            return result;
        }
        private static ApiResponse CallInsightsAPI(string applicationInsightsId, string applicationInsightsKey, string query)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://api.applicationinsights.io/v1/apps/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("type", "GET");
            client.DefaultRequestHeaders.Add("x-api-key", applicationInsightsKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(applicationInsightsId + query).Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ApiResponse>(response.Content.ReadAsStringAsync().Result);
            }
            return new ApiResponse() { tables = new Table[0] };
        }
        private static ApiResponse CallInsightsAPIV2(string applicationInsightsId, string query)
        {
            string bearerToken = GetBearerToken();

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://api.applicationinsights.io/v1/apps/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("type", "POST");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(applicationInsightsId + query).Result;

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ApiResponse>(response.Content.ReadAsStringAsync().Result);
            }
            return new ApiResponse() { tables = new Table[0] };
        }
        public static string DecodeBase64(string value)
        {
            var valueBytes = System.Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(valueBytes);
        }
    }
}
