using DeanOBrien.Foundation.DataAccess.Models;
using Newtonsoft.Json;
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
            if (!string.IsNullOrWhiteSpace(problemIdBase64) && !string.IsNullOrWhiteSpace(innerMostMessageBase64)) return GetGroupedExceptions(applicationInsightsId, applicationInsightsKey, problemIdBase64, null, timespan).Where(x => x.InnerMostMessageBase64 == innerMostMessageBase64).ToList();

            string query = BuildDetailedQuery(problemIdBase64, innerMostMessageBase64, timespan);
            var apiResponse = CallInsightsAPI(applicationInsightsId, applicationInsightsKey, query);
            return DeserialiseResponse(apiResponse, true);
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

        private static List<GroupedException> DeserialiseResponse(ApiResponse apiResponse, bool detailed = false)
        {
            var result = new List<GroupedException>();
            if (apiResponse.tables.Any() && apiResponse.tables[0] != null && apiResponse.tables[0].rows != null && apiResponse.tables[0].rows.Count() > 0)
            {
                apiResponse.tables[0].rows.ToList().ForEach(x => result.Add(new GroupedException(JsonConvert.DeserializeObject<object[]>(x.ToString()), detailed)));
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
        public static string DecodeBase64(string value)
        {
            var valueBytes = System.Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(valueBytes);
        }
    }
}
