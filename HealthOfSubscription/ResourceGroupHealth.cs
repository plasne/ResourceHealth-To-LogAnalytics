using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace HealthOfSubscription
{
    public static class ResourceGroupHealth
    {
        [FunctionName("ResourceGroupHealth")]
        [StorageAccount("QueueStorageConnString")]
        public static void Run([QueueTrigger("resourcegrouphealthmonitor", Connection = "QueueStorageConnString")]string myQueueItem, TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
