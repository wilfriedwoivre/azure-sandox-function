using './azuredeploy.bicep'

// Resource Group Manager custom role, use built-in role 'Contributor' if you don't have dedicated role
param roleDefinitionId = '956ccb9e-f262-4480-9b2d-8e69685c9810'
