@description('Globally unique registry name. Five to fifty alphanumeric characters.')
@minLength(5)
@maxLength(50)
param name string

@description('Azure region for the registry.')
param location string

@description('Tags applied to the registry.')
param tags object

@description('Registry SKU. Basic is enough for a demo. Premium adds geo-replication, private endpoints and content trust.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Basic'

@description('Principal id of the identity that is allowed to pull images (AcrPull).')
param pullerPrincipalId string

var acrPullRoleDefinitionId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, pullerPrincipalId, acrPullRoleDefinitionId)
  scope: registry
  properties: {
    principalId: pullerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleDefinitionId)
  }
}

@description('Registry name.')
output name string = registry.name

@description('Registry login server, for example myregistry.azurecr.io.')
output loginServer string = registry.properties.loginServer
