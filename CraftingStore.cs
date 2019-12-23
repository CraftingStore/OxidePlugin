using System;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("CraftingStore", "CraftingStore.net", "0.1")]
    [Description("Handle CraftingStore commands")]

    class CraftingStore : RustPlugin
    {
        private string baseUrl = "https://api.craftingstore.net/v4/";

        private string apiToken = "";

        private bool pluginEnabled = true;

        void Loaded()
        {
            // Set token
            this.apiToken = Config["token"].ToString();

            if (this.apiToken == "Enter your API token") {
                Puts("Your API token is not yet set, please set the API token in the config and reload the CraftingStore plugin.");
                this.pluginEnabled = false;
            }

            if (this.pluginEnabled) {
                // Request commands on load
                RequestCommands();

                // Request commands
                timer.Repeat(240, 0, () =>
                {
                    RequestCommands();
                });
            }
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating CraftingStore Config");
            Config.Clear();
            Config["token"] = "Enter your API token";

            SaveConfig();
        }

        private ApiResponse ParseResponse(string response)
        {
            ApiResponse commands = JsonConvert.DeserializeObject<ApiResponse>(response);
            return commands;
        }

        private void GetRequest(string uri, string action)
        {
            // Set the authentication header
            Dictionary<string, string> headers = new Dictionary<string, string> { { "token", this.apiToken } };

            webrequest.Enqueue(this.baseUrl + uri, null, (code, response) =>
                GetCallback(code, response, action), this, Core.Libraries.RequestMethod.GET, headers);
        }

        private void GetCallback(int code, string response, string action)
        {
            if (response == null || code != 200)
            {
                Puts("Got error: Invalid response returned, please contact us if this error persists.");
                return;
            }

            // Create model from the JSON response.
            ApiResponse parsedResponse = ParseResponse(response);

            // Validate that the request got a success response back, if not, return the message.
            if (!parsedResponse.getSuccess()) {
                Puts("Got Error: " + parsedResponse.getMessage());
                return;
            }


            if (action == "queue") {
                this.ProcessQueuedCommands(parsedResponse);
            }
        }

        private void PostRequest(string uri, string action, string payload)
        {
            // Set the authentication header
            Dictionary<string, string> headers = new Dictionary<string, string> { { "token", this.apiToken } };

            webrequest.Enqueue(this.baseUrl + uri, payload, (code, response) =>
                PostCallback(code, response, action), this, Core.Libraries.RequestMethod.POST, headers);
        }

        private void PostCallback(int code, string response, string action)
        {
            if (response == null || code != 200)
            {
                Puts("Got error: Invalid response returned, please contact us if this error persists.");
                return;
            }

            // Create model from the JSON response.
            ApiResponse parsedResponse = ParseResponse(response);

            // Validate that the request got a success response back, if not, return the message.
            if (!parsedResponse.getSuccess()) {
                Puts("Got Error: " + parsedResponse.getMessage());
                return;
            }
        }

        private void ProcessQueuedCommands(ApiResponse parsedResponse)
        {
            QueueResponse[] donations = parsedResponse.result;

            List<int> ids = new List<int>();

            foreach (QueueResponse donation in donations)
            {
                // Add donation to executed list.
                ids.Add(donation.getId());

                // Execute commands
                Puts("Executing Command: " + donation.getCommand());
                rust.RunServerCommand(donation.getCommand());
            }

            if (ids.Count > 0) {
                // Mark as complete if there are commands processed
                string serializedIds = JsonConvert.SerializeObject(ids);

                string payload = "removeIds=" + serializedIds;

                PostRequest("queue/markComplete", "markComplete", payload);
            }
        }

        private void RequestCommands()
        {
            GetRequest("queue", "queue");
        }
        
        public class QueueResponse {

            public int id;
            public string command;
            public string packageName;

            public int getId()
            {
                return this.id;
            }

            public string getCommand()
            {
                return this.command;
            }

            public string getPackageName()
            {
                return this.packageName;
            }
        }

        public class ApiResponse {

            public int id;
            public bool success;
            public string error;
            public string message;
            public QueueResponse[] result;

            public int getId()
            {
                return this.id;
            }

            public bool getSuccess()
            {
                return this.success;
            }

            public string getError()
            {
                return this.error;
            }

            public string getMessage()
            {
                return this.message;
            }

            public QueueResponse[] getResult()
            {
                return this.result;
            }
        }

    }
}
