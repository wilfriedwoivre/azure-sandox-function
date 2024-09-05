targetScope='subscription'

param resourceGroupName string='automatic-sandbox-rg'

@description('The location of resource group and resources')
param location string = deployment().location

@description('roleDefinition for the assignment - default is contributor')
param roleDefinitionId string = 'b24988ac-6180-42a0-ab88-20f7382dd24c'


resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: {
    environment: 'sandbox'
    githubLink: 'https://github.com/wilfriedwoivre/azure-sandbox-function'
  }
}

module stg 'br/public:avm/res/storage/storage-account:0.11.1' = {
  name: 'sandbox-storage'
  scope: resourceGroup
  params: {
    name: 'stg${uniqueString(resourceGroup.id)}'
    skuName: 'Standard_LRS'
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

module appInsight 'br/public:avm/res/insights/component:0.4.0' = {
  scope: resourceGroup
  name: 'sandbox-insight'
  params: {
    name: 'insight${uniqueString(resourceGroup.id)}'
    applicationType: 'web'
    kind: 'web'
    location: resourceGroup.location
    workspaceResourceId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/common-rg/providers/Microsoft.OperationalInsights/workspaces/${subscription().subscriptionId}-law'
  }
}

module hostingPlan 'br/public:avm/res/web/serverfarm:0.2.2' = {
  scope: resourceGroup
  name: 'serverfarm'
  params: {
    name: 'plan${uniqueString(resourceGroup.id)}'
    skuCapacity: 1
    skuName: 'Y1'
    kind: 'Linux'
  }
}

module functionApp 'br/public:avm/res/web/site:0.4.0' = {
  scope: resourceGroup
  name: 'functionapp'
  params: {
    name: 'sandbox${uniqueString(resourceGroup.id)}'
    kind: 'functionapp,linux'
    serverFarmResourceId: hostingPlan.outputs.resourceId
    storageAccountResourceId: stg.outputs.resourceId
    appSettingsKeyValuePairs: {
      'FUNCTIONS_EXTENSION_VERSION': '~4'
      'FUNCTIONS_WORKER_RUNTIME': 'dotnet-isolated'
      'WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED': '1'
    }
    storageAccountUseIdentityAuthentication: false
    appInsightResourceId: appInsight.outputs.resourceId
    enableTelemetry: true
    httpsOnly: true
    siteConfig: {
      alwaysOn: false
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
    }
    managedIdentities: {
      systemAssigned: true
    }
  }
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2020-08-01-preview' = {
  name:  guid(subscription().id, functionApp.name, roleDefinitionId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
    principalId: functionApp.outputs.systemAssignedMIPrincipalId
  }
}
