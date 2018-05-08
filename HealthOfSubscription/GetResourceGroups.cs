using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Rest.Azure.Authentication;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Models;
using System.Collections.Generic;
using Microsoft.Rest.Azure;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;

namespace HealthOfSubscription
{
    public static class GetResourceGroups
    {
        [FunctionName("GetResourceGroups")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            var clientId = System.Environment.GetEnvironmentVariable("AADClientId", EnvironmentVariableTarget.Process);
            var secret = System.Environment.GetEnvironmentVariable("AADClientSecret", EnvironmentVariableTarget.Process);
            var tenantId = System.Environment.GetEnvironmentVariable("AADTenantId", EnvironmentVariableTarget.Process);
            var subscriptionId = System.Environment.GetEnvironmentVariable("SubscriptionId", EnvironmentVariableTarget.Process);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("QueueStorageConnString", EnvironmentVariableTarget.Process));
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Build the service credentials and Azure Resource Manager clients
            //var serviceCreds = ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, secret).Result;
            var serviceCreds = Task.Run(() => ApplicationTokenProvider.LoginSilentAsync("094cfb7a-d131-4637-9047-e339e7d04359", clientId, secret)).Result;
            var resourceClient = new ResourceManagementClient(serviceCreds);
            resourceClient.SubscriptionId = subscriptionId;
            IPage<ResourceGroup> resourceGroups = resourceClient.ResourceGroups.List();
            string nextPageLink = null;

            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference("resourcegroup-healthmonitor");

            // Create the queue if it doesn't already exist.
            queue.CreateIfNotExists();

            do
            {
                foreach(var rg in resourceGroups)
                {
                    CloudQueueMessage message = new CloudQueueMessage(rg.Id);
                    queue.AddMessage(message);
                    log.Info("ResourceGroup: " + rg.Id);
                }
                nextPageLink = resourceGroups.NextPageLink;
                
            }
            while (nextPageLink != null && (resourceGroups = resourceClient.ResourceGroups.ListNext(nextPageLink)) != null) ;


           //     log.Info("Hello World");
        }
    }
}
