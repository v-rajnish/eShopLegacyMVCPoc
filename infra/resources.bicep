// =====================================================================================
// eShopLegacyMVC -> Azure : Phase 4 Infrastructure (resource-group scope)
// -------------------------------------------------------------------------------------
// App Service (Linux, .NET 8) + Azure SQL (Entra-only auth) + Key Vault +
// Application Insights + Log Analytics + User-Assigned Managed Identity.
// SQL authentication is Microsoft Entra ID + Managed Identity (no SQL passwords).
// =====================================================================================

@description('Primary Azure region.')
param location string

@description('Short environment/stage name used in resource naming.')
param environmentName string

@description('Common resource tags.')
param tags object

@description('PLACEHOLDER: Entra ID admin display name / UPN for Azure SQL.')
param sqlAadAdminName string

@description('PLACEHOLDER: Entra ID admin object ID (GUID) for Azure SQL.')
param sqlAadAdminObjectId string

@description('Azure SQL database name.')
param sqlDatabaseName string

@description('App Service Plan SKU. B1 (Basic) is required for VNet integration to reach the private SQL endpoint.')
param appServicePlanSku string = 'B1'

@description('Azure SQL database SKU name.')
param sqlDatabaseSku string = 'S0'

@secure()
@description('Random value regenerated each deployment, stored as a demo Key Vault secret.')
param randomSecretSeed string = newGuid()

// Deterministic unique token for globally-unique names
var resourceToken = uniqueString(subscription().id, resourceGroup().id, environmentName)
var prefix = 'eshop'

var identityName = '${prefix}-id-${resourceToken}'
var logAnalyticsName = '${prefix}-log-${resourceToken}'
var appInsightsName = '${prefix}-appi-${resourceToken}'
var vnetName = '${prefix}-vnet-${resourceToken}'

// Free/Shared tiers (F1/D1) don't support Always On and run on shared workers.
var isDedicatedPlan = !contains(['F1', 'D1'], appServicePlanSku)
var keyVaultName = take('${prefix}kv${resourceToken}', 24)
var appServicePlanName = '${prefix}-plan-${resourceToken}'
var webAppName = '${prefix}-web-${resourceToken}'
var sqlServerName = '${prefix}-sql-${resourceToken}'

// ---------------------------------------------------------------------------
// User-assigned managed identity (used by App Service for SQL + Key Vault)
// ---------------------------------------------------------------------------
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

// ---------------------------------------------------------------------------
// Log Analytics + Application Insights (workspace-based)
// ---------------------------------------------------------------------------
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ---------------------------------------------------------------------------
// Key Vault (access-policy authorization)
// ---------------------------------------------------------------------------
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    // Private-endpoint-only (org policy forces this regardless). Reachable from
    // inside the VNet via the private endpoint defined below.
    publicNetworkAccess: 'Disabled'
    // Grant the managed identity read access to secrets via access policy
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: identity.properties.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
      {
        // Grants the SQL Entra admin (the developer running this POC) secret read
        // access so secrets can be inspected locally, e.g. `az keyvault secret show`.
        tenantId: subscription().tenantId
        objectId: sqlAadAdminObjectId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

// ---------------------------------------------------------------------------
// Azure SQL Server (Entra-only authentication) + Database
// ---------------------------------------------------------------------------
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
    primaryUserAssignedIdentityId: identity.id
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: sqlAadAdminName
      sid: sqlAadAdminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: sqlDatabaseSku
    tier: 'Standard'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
  }
}

// ---------------------------------------------------------------------------
// Virtual Network: subnet for the SQL private endpoint + subnet delegated to
// App Service for regional VNet integration.
// ---------------------------------------------------------------------------
resource vnet 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.10.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'snet-pe'
        properties: {
          addressPrefix: '10.10.1.0/24'
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
      {
        name: 'snet-app'
        properties: {
          addressPrefix: '10.10.2.0/24'
          delegations: [
            {
              name: 'webapp-delegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
    ]
  }
}

// Private DNS zone for SQL private link + link to the VNet
resource sqlPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink${environment().suffixes.sqlServerHostname}'
  location: 'global'
  tags: tags
}

resource sqlDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: sqlPrivateDnsZone
  name: '${vnetName}-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

// Private endpoint for the SQL server (public access is disabled by policy)
resource sqlPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: '${sqlServerName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: '${vnet.id}/subnets/snet-pe'
    }
    privateLinkServiceConnections: [
      {
        name: '${sqlServerName}-plsc'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: [
            'sqlServer'
          ]
        }
      }
    ]
  }
}

resource sqlPeDnsGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = {
  parent: sqlPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'sql'
        properties: {
          privateDnsZoneId: sqlPrivateDnsZone.id
        }
      }
    ]
  }
}

// Private DNS zone for Key Vault private link + link to the VNet
resource keyVaultPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.vaultcore.azure.net'
  location: 'global'
  tags: tags
}

resource keyVaultDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: keyVaultPrivateDnsZone
  name: '${vnetName}-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

// Private endpoint for Key Vault (public access is disabled by policy)
resource keyVaultPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: '${keyVaultName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: '${vnet.id}/subnets/snet-pe'
    }
    privateLinkServiceConnections: [
      {
        name: '${keyVaultName}-plsc'
        properties: {
          privateLinkServiceId: keyVault.id
          groupIds: [
            'vault'
          ]
        }
      }
    ]
  }
}

resource keyVaultPeDnsGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = {
  parent: keyVaultPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'vault'
        properties: {
          privateDnsZoneId: keyVaultPrivateDnsZone.id
        }
      }
    ]
  }
}

// Passwordless SQL connection string (Entra ID via user-assigned managed identity).
var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabaseName};Authentication=Active Directory Default;User Id=${identity.properties.clientId};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

// ---------------------------------------------------------------------------
// Key Vault secrets
// - sql-connection-string: read by App Service natively via a Key Vault
//   reference app setting (no SDK code needed in the app).
// - demo-api-key: a sample value read directly by the app at startup using
//   the Azure Key Vault SDK + DefaultAzureCredential, to demonstrate that pattern.
// ---------------------------------------------------------------------------
resource sqlConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'sql-connection-string'
  properties: {
    value: sqlConnectionString
  }
}

resource demoApiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'demo-api-key'
  properties: {
    value: 'demo-secret-value-${uniqueString(resourceGroup().id, deployment().name)}'
  }
}

// Random secret regenerated on every deployment, displayed on the app's Key Vault demo page.
resource demoRandomKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'demo-random-key'
  properties: {
    value: randomSecretSeed
  }
}

// ---------------------------------------------------------------------------
// App Service Plan (Linux) + Web App (.NET 8)
// ---------------------------------------------------------------------------
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: appServicePlanSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}



resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  tags: union(tags, {
    'azd-service-name': 'web'
  })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    keyVaultReferenceIdentity: identity.id
    virtualNetworkSubnetId: '${vnet.id}/subnets/snet-app'
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: isDedicatedPlan
      vnetRouteAllEnabled: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      healthCheckPath: '/'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'UseMockData'
          value: 'true'
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: identity.properties.clientId
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${sqlConnectionSecret.properties.secretUri})'
        }
        {
          name: 'KeyVaultUri'
          value: keyVault.properties.vaultUri
        }
      ]
    }
  }
  dependsOn: [
    sqlPeDnsGroup
    keyVaultPeDnsGroup
  ]
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------
output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output managedIdentityName string = identity.name
output managedIdentityClientId string = identity.properties.clientId
output managedIdentityPrincipalId string = identity.properties.principalId
output appInsightsName string = appInsights.name
