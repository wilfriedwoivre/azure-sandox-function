using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text.Json;

namespace wwoivre.azure.sandbox.tooling
{
    public class CreateResourceGroup
    {
        private readonly ILogger<CreateResourceGroup> _logger;

        public CreateResourceGroup(ILogger<CreateResourceGroup> logger)
        {
            _logger = logger;
        }

        [Function("CreateResourceGroup")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                var data = JsonSerializer.Deserialize<CreateResourceGroupRequest>(requestBody);

                if (data.ExpirationDate == default(DateTime)) data.ExpirationDate = DateTime.UtcNow;

                ArmClient client = new ArmClient(new DefaultAzureCredential());
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
                ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();

                AzureLocation location = AzureLocation.WestEurope;
                string resourceGroupName = data.Name;

                ResourceGroupData resourceGroupData = new ResourceGroupData(location);
                ArmOperation<ResourceGroupResource> resourceGroupOperation = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, resourceGroupData);
                ResourceGroupResource resourceGroup = resourceGroupOperation.Value;

                var tags = new Dictionary<string, string>()
                {
                    { "AutoDelete", "true" },
                    { "ExpirationDate", data.ExpirationDate.ToString("yyyy-MM-dd") }
                };
                await resourceGroup.SetTagsAsync(tags);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

            return new OkObjectResult("Success");
        }
    }

    public class CreateResourceGroupRequest
    {
        public string Name { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
