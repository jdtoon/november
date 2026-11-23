@description('Globally unique vault name. Three to twenty-four characters.')
@minLength(3)
@maxLength(24)
param name string

@description('Azure region for the vault.')
param location string

@description('Tags applied to the vault.')
param tags object

@description('Principal id of the identity that may read secrets (Key Vault Secrets User).')
param vaultReaderPrincipalId string

@description('Turn purge protection on. It cannot be turned off again once enabled, so dev leaves it off and prod turns it on.')
param enablePurgeProtection bool

@description('Days a deleted vault or secret stays recoverable.')
@minValue(7)
@maxValue(90)
param softDeleteRetentionInDays int = 90

@description('Name of the secret that holds the Application Insights connection string.')
param appInsightsSecretName string = 'appinsights-connection-string'

@description('Application Insights connection string to store as a secret.')
@secure()
param appInsightsConnectionString string

var keyVaultSecretsUserRoleDefinitionId = '4633458b-17de-408a-b874-0445c86b69e6'

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enablePurgeProtection: enablePurgeProtection ? true : null
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

resource appInsightsSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: appInsightsSecretName
  properties: {
    value: appInsightsConnectionString
    contentType: 'text/plain'
  }
}

resource secretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, vaultReaderPrincipalId, keyVaultSecretsUserRoleDefinitionId)
  scope: vault
  properties: {
    principalId: vaultReaderPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleDefinitionId)
  }
}

@description('Vault name.')
output name string = vault.name

@description('Version-less URL of the Application Insights secret. Container Apps follow the latest version.')
output appInsightsSecretUrl string = '${vault.properties.vaultUri}secrets/${appInsightsSecret.name}'
