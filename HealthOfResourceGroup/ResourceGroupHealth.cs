using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Management.ResourceManager;
using System.Collections.Specialized;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using HealthOfResourceGroup;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;

namespace HealthOfSubscription
{
    public static class ResourceGroupHealth
    {
        [FunctionName("ResourceGroupHealth")]
        [StorageAccount("QueueStorageConnString")]
        public static void Run([QueueTrigger("resourcegroup-healthmonitor", Connection = "QueueStorageConnString")]string myQueueItem, TraceWriter log)
        {
            //GET https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ResourceHealth/availabilityStatuses?api-version=2015-01-01
            log.Info($"C# Queue trigger function processed: {myQueueItem}");
            var clientId = System.Environment.GetEnvironmentVariable("AADClientId", EnvironmentVariableTarget.Process);
            var secret = System.Environment.GetEnvironmentVariable("AADClientSecret", EnvironmentVariableTarget.Process);
            var tenantId = System.Environment.GetEnvironmentVariable("AADTenantId", EnvironmentVariableTarget.Process);
            var subscriptionId = System.Environment.GetEnvironmentVariable("SubscriptionId", EnvironmentVariableTarget.Process);
            var resourceGroupArray = myQueueItem.Split('/');
            var resourceGroupName = resourceGroupArray[resourceGroupArray.Length - 1];

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("QueueStorageConnString", EnvironmentVariableTarget.Process));
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            var queueName = "resource-healthmonitor";
            CloudQueue queue = queueClient.GetQueueReference(queueName);

            // Create the queue if it doesn't already exist.
            queue.CreateIfNotExists();

            var token = GetAccessToken(clientId, secret, tenantId, myQueueItem);
            if(token == null)
            {
                log.Error("Error getting access token.");
            }

            var resourcesHealth = GetResourceGroupHealth(myQueueItem, token, log);
            if(resourcesHealth == null)
            {
                log.Error("Error getting Resource Health for Resource Group: " + myQueueItem);
            }
            else if (resourcesHealth.Count == 0)
            {
                log.Warning("No resources found in Resource Group: " + myQueueItem);
            }
            else
            {
                foreach(AvailabilityStatus status in resourcesHealth)
                {
                    if(status.properties.availabilityStateValue == AvailabilityStateValue.Unavailable || status.properties.availabilityStateValue == AvailabilityStateValue.Unknown)
                    {
                        //Sending the availability information to Storage Queues
                        var jsonStr = JsonConvert.SerializeObject(status);
                        string resourceHealthUri = "/providers/Microsoft.ResourceHealth/availabilityStatuses/current";
                        var resourceId = status.id.Substring(0, status.id.Length - resourceHealthUri.Length);
                        CloudQueueMessage message = new CloudQueueMessage(resourceId, "");
                        message.SetMessageContent(jsonStr);
                        queue.AddMessage(message);
                    }
                }
            }

        }

        public static List<AvailabilityStatus> GetResourceGroupHealth(string resourceGroupId, string authToken, TraceWriter log)
        {
            HttpClient hc = new HttpClient();
            string healthUrl = "https://management.azure.com" + resourceGroupId + "/providers/Microsoft.ResourceHealth/availabilityStatuses?api-version=2015-01-01";
            List<AvailabilityStatus> resourceAvailibility = new List<AvailabilityStatus>();
            while(healthUrl != null)
            {
                hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                HttpResponseMessage hrm = hc.GetAsync(healthUrl).Result;
                string responseData = "";
                if (hrm.IsSuccessStatusCode)
                {
                    Stream data = hrm.Content.ReadAsStreamAsync().Result;
                    using (StreamReader reader = new StreamReader(data, Encoding.UTF8))
                    {
                        responseData = reader.ReadToEnd();
                        ResourceHealth parsedResponse = JsonConvert.DeserializeObject<ResourceHealth>(responseData);
                        if (parsedResponse != null && parsedResponse.value.Length > 0)
                        {
                            if (parsedResponse.nextLink != null && !parsedResponse.nextLink.Equals(""))
                            {
                                healthUrl = parsedResponse.nextLink;
                            }
                            else
                            {
                                healthUrl = null;
                            }
                            resourceAvailibility.AddRange(parsedResponse.value);
                        }
                        else
                        {
                            //Empty Resource group
                            healthUrl = null;
                        }
                    }
                }
                else
                {
                    //Did not get a 200 OK. Eventually, you will want to add retry calls.
                    healthUrl = null;
                }
            }

            return resourceAvailibility;
        }

        public static string GetAccessToken(string clientId, string clientSecret, string tenantId, string myQueueItem)
        {
            // Create client cradentials.
            var credential = new ClientCredential(clientId, clientSecret);
            var resourceUrl = "https://management.core.windows.net/";

            // Authenticate using created credentials
            string url = string.Format("https://login.windows.net/{0}/oauth2/token", tenantId);
            var authenticationContext = new AuthenticationContext(url);
            var authenticationResult = authenticationContext.AcquireTokenAsync(resourceUrl, credential).Result;

            if (authenticationResult == null)
            {
                return null;
            }
            return authenticationResult.AccessToken;
        }
    }
}
