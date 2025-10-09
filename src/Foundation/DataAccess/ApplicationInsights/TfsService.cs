using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights.ApplicationInsights
{
    public class TfsService : ITfsService
    {
        private static string _organization;
        private static string _project;
        private static string _repoId;
        private static string _pat;
        private static string _baseBranch;
        private static string _baseUrl;
        private static string _baseUrlWit;

        public TfsService()
        {

        }
        public void Initialize(string organization, string project, string repoId, string pat)
        {
            _organization = organization;
            _project = project;
            _repoId = repoId;
            _pat = pat;
            _baseBranch = "heads/release";
            _baseUrl = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repoId}";
            _baseUrlWit = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/$Product%20Backlog%20Item";

        }
        public async Task<string> GetFile(string fileName)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            System.Text.ASCIIEncoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", "", _pat))));

                    using (HttpResponseMessage response = client.GetAsync(
                    $"https://dev.azure.com/{_organization}/{_project}/_apis/git/repositories/{_repoId}/items?path={fileName}&download=true&api-version=5.0&includeContent=true").Result)
                    {
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic jsonData = JsonConvert.DeserializeObject(responseBody);

                        var responseContent = jsonData.content;
                        //Console.WriteLine(responseContent);
                        //Console.ReadLine();
                        return responseContent;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return "fail";
        }
        public async Task<int> CreateBacklog(string content, string problemId)
        {
            using (HttpClient client = new HttpClient())
            {
                AddHeaders(client);
                var jsonContent = new object[]
                {
                    new { op = "add", path = "/fields/System.Title", value = $"AI Generated: {problemId}" },
                    new { op = "add", path = "/fields/System.Description", value = content },
                    new { op = "add", path = "/fields/Microsoft.VSTS.Common.Priority", value = 2 }
                };
                var serialized = JsonConvert.SerializeObject(jsonContent);

                client.Timeout = TimeSpan.FromSeconds(10);

                try
                {
                    using (HttpResponseMessage response = client.PostAsync($"{_baseUrlWit}?api-version=7.1-preview.2",
                        new StringContent(serialized, Encoding.UTF8, "application/json-patch+json")).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var jsonResponse = JObject.Parse(responseContent);
                            var id = jsonResponse["id"]?.Value<int>();
                            return id ?? 0;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"HTTP Error: {ex.Message}");
                }
                catch (TaskCanceledException ex)
                {
                    Console.WriteLine($"Request timed out. {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Other error: {ex.Message}");
                }
            }
            return 0;
        }
        public async Task<bool> CreateBranchAndAddCommit(string newBranch, string filePath, string fileContent)
        {
            filePath = filePath.Replace("\\", "/");
            using (HttpClient client = new HttpClient())
            {
                AddHeaders(client);

                string getMainRefUrl = $"https://dev.azure.com/{_organization}/{_project}/_apis/git/repositories/{_repoId}/refs?filter={_baseBranch}&api-version=7.1";

                using (HttpResponseMessage refResp = client.GetAsync(getMainRefUrl).Result)
                {
                    refResp.EnsureSuccessStatusCode();
                    string responseBody = await refResp.Content.ReadAsStringAsync();
                    dynamic jsonData = JsonConvert.DeserializeObject<dynamic>(responseBody);

                    string firstObjectId = jsonData.value[0].objectId;
                    Console.WriteLine(firstObjectId);

                    var createBranchBody = new
                    {
                        refUpdates = new[]
                        {
                                new
                                {
                                    name = $"refs/heads/ai-fixes/{newBranch}",
                                    oldObjectId = firstObjectId
                                }
                            },
                        commits = new[]
                        {
                                new
                                {
                                    comment = $"AI Fix for {filePath}",
                                    changes = new[]
                                    {
                                        new
                                        {
                                            changeType = "edit",
                                            item = new { path = filePath },
                                            newContent = new
                                            {
                                                content = fileContent,
                                                contentType = "rawtext"
                                            }
                                        }
                                    }
                                }
                            }
                    };
                    var size = Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(createBranchBody));

                    using (HttpResponseMessage response = client.PostAsync($"{_baseUrl}/pushes?api-version=7.1",
                        new StringContent(JsonConvert.SerializeObject(createBranchBody), Encoding.UTF8, "application/json")).Result)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        response.EnsureSuccessStatusCode();
                    }
                    return true;
                }
            }
        }

        private static void AddHeaders(HttpClient client)
        {
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_pat}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }


        public async Task<string> PostToApi(string url, string json)
        {
            using (HttpClient client = new HttpClient())
            {
                AddHeaders(client);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                using (HttpResponseMessage response = client.PostAsync(url, content).Result)
                {
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
            return string.Empty;
        }

        public async Task<string> GetFromApi(string url, string json)
        {
            using (HttpClient client = new HttpClient())
            {
                AddHeaders(client);
                using (HttpResponseMessage response = client.GetAsync(url).Result)
                {
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
            return string.Empty;
        }
        public async Task<int> GetWorkItemId(string problemId)
        {
            var workItemTitle = $"AI Generated: {problemId}";
            var url = $"https://dev.azure.com/{_organization}/{_project}/_apis/wit/wiql?api-version=7.0";
            var wiqlQuery = new
            {
                query = $@"
                SELECT [System.Id], [System.Title]
                FROM WorkItems
                WHERE [System.TeamProject] = '{_project}'
                  AND [System.Title] = '{workItemTitle}'
                  AND [System.WorkItemType] <> ''
                  AND [System.State] <> 'Removed'
                  ORDER BY [System.ChangedDate] DESC"
            };
            var json = JsonConvert.SerializeObject(wiqlQuery);

            var result = await PostToApi(url, json);

            var parsed = JObject.Parse(result);
            var items = parsed["workItems"];

            if (items.HasValues && items.Count() >= 1) return Convert.ToInt32(items.FirstOrDefault()["id"]?.ToString());
            return 0;
        }

        public async Task<string> GetWorkItemDescription(int id)
        {
            string url = $"https://dev.azure.com/{_organization}/{_project}/_apis/wit/workitems?ids={id}&api-version=7.0";

            //var detailsResponse = await client.GetAsync(detailsUri);

            var result = await GetFromApi(url, null);

            var detailsObj = JObject.Parse(result);
            var items = detailsObj["value"] as JArray;
            var item = items.FirstOrDefault();
            return item["fields"]?["System.Description"]?.ToString() ?? string.Empty;
        }
        public async Task<int> CreatePR(string problemId, string sourceBranch, string targetBranch, int workItem = 0)
        {
            var workItems = workItem == 0 ? null : new[] { new { id = workItem } };

            var createPrUri = $"https://dev.azure.com/{_organization}/{_project}/_apis/git/repositories/{_repoId}/pullrequests?api-version=7.0";
            var prPayload = new
            {
                sourceRefName = $"refs/heads/ai-fixes/{sourceBranch}",
                targetRefName = $"refs/heads/{targetBranch}",
                title = $"PR to merge fix for {problemId}",
                description = "PR created via API",
                workItemRefs = workItems
            };
            var json = JsonConvert.SerializeObject(prPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                AddHeaders(client);
                using (HttpResponseMessage createPrResponse = client.PostAsync(createPrUri, content).Result)
                {
                    var prResponseJson = await createPrResponse.Content.ReadAsStringAsync();
                    if (createPrResponse.IsSuccessStatusCode)
                    {
                        var prObj = JObject.Parse(prResponseJson);
                        var prId = prObj["pullRequestId"]?.Value<int>();
                        return prId ?? 0;
                    }
                }
            }
            return 0;
        }

    }
}
