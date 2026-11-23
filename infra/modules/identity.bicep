@description('Name of the user-assigned managed identity.')
param name string

@description('Azure region for the identity.')
param location string

@description('Tags applied to the identity.')
param tags object

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
  tags: tags
}

@description('Resource id of the identity. Container Apps reference this to pull images and read Key Vault secrets.')
output id string = identity.id

@description('Object (principal) id of the identity. Used as the principal in role assignments.')
output principalId string = identity.properties.principalId

@description('Client id of the identity.')
output clientId string = identity.properties.clientId
