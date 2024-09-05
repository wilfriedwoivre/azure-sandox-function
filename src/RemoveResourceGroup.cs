using System;
using System.Globalization;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace wwoivre.azure.sandbox.tooling
{
    public class RemoveResourceGroup
    {
        private readonly ILogger _logger;

        public RemoveResourceGroup(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RemoveResourceGroup>();
        }

        [Function("RemoveResourceGroup")]
        public async Task Run([TimerTrigger("0 0 4 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }

            ArmClient client = new ArmClient(new DefaultAzureCredential());
            SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();

            await foreach (var resourceGroup in resourceGroups.GetAllAsync("tagName eq 'AutoDelete' and tagValue eq 'true'"))
            {
                string expirationDate;
                if (resourceGroup.Data.Tags.TryGetValue("ExpirationDate", out expirationDate))
                {
                    var date = DateTime.Parse(expirationDate, CultureInfo.GetCultureInfo("en-US"));
                    if (date < DateTime.UtcNow)
                    {
                        await resourceGroup.DeleteAsync(Azure.WaitUntil.Started);
                    }
                }
            }
        }
    }
}
