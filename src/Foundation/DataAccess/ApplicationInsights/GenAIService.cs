using Azure.AI.OpenAI;
using OpenAI.Chat;
using Sitecore.Diagnostics;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights.ApplicationInsights
{
    public class GenAIService : IGenAIService
    {
        public bool IsReady;
        public string ErrorMessage;

        private const string SettingsId = "{C34468E8-0CED-4F4D-8F5D-B52F1604E145}";
        private const string DeployedModels = "{B09A35E2-B289-423D-A8CD-914E7AA58166}";
        private string _deployedModel;
        private string _endpoint;
        private string _key;
        private string _languageModelName;
        private string _searchEndpoint = null;
        private string _searchIndex = null;
        private string _searchKey = null;

        public GenAIService()
        {
            Log.Info("GenAIService()", "GenAIService");

            IsReady = false;
            try
            {
                var database = Sitecore.Configuration.Factory.GetDatabase("web");
                var settingsItem = database.GetItem(SettingsId);
                if (settingsItem == null) ErrorMessage = "There seems to be an issue with configuration: Settings Item is missing";

                var deployedModelsItem = database.GetItem(DeployedModels);
                if (deployedModelsItem == null) ErrorMessage = "There seems to be an issue with configuration: Deployed Models Folder is missing";
                if (deployedModelsItem.Children.Count() == 0) ErrorMessage = "There seems to be an issue with configuration: No deployed models have been added";

                var deployedModels = deployedModelsItem.Children.Select(x => x.Fields["Model Name"].Value).ToList();
                _languageModelName = settingsItem.Fields["Default Model"].Value;

                if (string.IsNullOrWhiteSpace(_languageModelName) || !deployedModels.Where(model => model == _languageModelName).Any())
                {
                    _languageModelName = deployedModels.FirstOrDefault();
                }

                var languageModelItem = deployedModelsItem.Children.Where(model => model.Fields["Model Name"].Value == _languageModelName).FirstOrDefault();
                if (languageModelItem == null) ErrorMessage = "There seems to be an issue with configuration: Could not retrieve deployed model";

                _endpoint = languageModelItem.Fields["Endpoint"].Value;
                if (string.IsNullOrWhiteSpace(_endpoint)) ErrorMessage = "There seems to be an issue with configuration: No Endpoint configured for the deployed model";

                _key = languageModelItem.Fields["Key"].Value;
                if (string.IsNullOrWhiteSpace(_key)) ErrorMessage = "There seems to be an issue with configuration: No Key configured for the deployed model";
                IsReady = true;
                Log.Info("GenAI ready", "GenAI");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Log.Info($"GenAI not ready: {ErrorMessage}", "GenAI");
            }
        }

        public string Call(List<Tuple<string, string>> prompts, string userPrompt = "", string context = "")
        {
            Log.Info("Call()", "GenAIService");
            try
            {
                AzureOpenAIClient azureClient = new AzureOpenAIClient(
                    new Uri(_endpoint),
                    new ApiKeyCredential(_key));
                ChatClient chatClient = azureClient.GetChatClient(_languageModelName);

                string mainSystemPrompt = string.Empty;
                if (!string.IsNullOrWhiteSpace(context)) mainSystemPrompt = "Please review the following text and ";
                if (!string.IsNullOrWhiteSpace(userPrompt)) mainSystemPrompt += userPrompt;
                if (!string.IsNullOrWhiteSpace(context)) mainSystemPrompt = "using the following text:" + context;

                var messages = new List<ChatMessage>();

                foreach (var prompt in prompts)
                {
                    if (prompt.Item1 == "System") messages.Add(new SystemChatMessage(prompt.Item2));
                    else if (prompt.Item1 == "User") messages.Add(new UserChatMessage(prompt.Item2));
                }
                messages.Add(new UserChatMessage(mainSystemPrompt));

                ChatCompletion completion = chatClient.CompleteChat(messages);
                return completion.Content[0].Text;
            }
            catch (Exception ex)
            {
                Log.Info($"Failed to call model ({_languageModelName}) at endpoint ({_endpoint})\r\n {ex.Message} ",this);
                throw;
            }
        }
    }
}
