// =====================================================================================
// eShopLegacyMVC -> Azure : Phase 4 Infrastructure (subscription-scope orchestrator)
// -------------------------------------------------------------------------------------
// Creates the target resource group and deploys all application resources.
// NOTE: Subscription is intentionally NOT hard-coded. Select it at deploy time:
//   az account set --subscription "<CAF-SUBSCRIPTION-ID>"   (provided by CAF team later)
// =====================================================================================
targetScope = 'subscription'

@minLength(1)
@description('Short environment/stage name used to build resource names (e.g. poc, dev, prod).')
param environmentName string = 'poc'

@description('Primary Azure region for all resources.')
param location string = 'eastus'

@description('Name of the target resource group.')
param resourceGroupName string = 'rg-eshop-poc'

@description('PLACEHOLDER: Display name / UPN of the Microsoft Entra ID user or group that becomes the Azure SQL administrator.')
param sqlAadAdminName string = 'PLACEHOLDER-entra-admin@contoso.com'

@description('PLACEHOLDER: Object ID (GUID) of the Microsoft Entra ID admin principal for Azure SQL.')
param sqlAadAdminObjectId string = '00000000-0000-0000-0000-000000000000'

@description('Name of the Azure SQL database.')
param sqlDatabaseName string = 'eShopPorted'

var tags = {
  application: 'eShopLegacyMVC'
  environment: environmentName
  managedBy: 'bicep'
}

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  name: 'eshop-resources'
  scope: rg
  params: {
    location: location
    environmentName: environmentName
    tags: tags
    sqlAadAdminName: sqlAadAdminName
    sqlAadAdminObjectId: sqlAadAdminObjectId
    sqlDatabaseName: sqlDatabaseName
  }
}

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output WEB_APP_NAME string = resources.outputs.webAppName
output WEB_APP_URL string = resources.outputs.webAppUrl
output SQL_SERVER_FQDN string = resources.outputs.sqlServerFqdn
output SQL_DATABASE_NAME string = resources.outputs.sqlDatabaseName
output KEY_VAULT_NAME string = resources.outputs.keyVaultName
output MANAGED_IDENTITY_NAME string = resources.outputs.managedIdentityName
output MANAGED_IDENTITY_CLIENT_ID string = resources.outputs.managedIdentityClientId
output APPLICATION_INSIGHTS_NAME string = resources.outputs.appInsightsName

