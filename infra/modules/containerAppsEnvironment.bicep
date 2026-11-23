@description('Name of the Container Apps environment.')
param name string

@description('Azure region for the environment.')
param location string

@description('Tags applied to the environment.')
param tags object

@description('Name of the Log Analytics workspace that receives console and system logs.')
param logAnalyticsWorkspaceName string

@description('Encrypt traffic between apps in the environment with mutual TLS.')
param peerTrafficEncryption bool = true

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: workspace.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    peerTrafficConfiguration: {
      encryption: {
        enabled: peerTrafficEncryption
      }
    }
    zoneRedundant: false
  }
}

@description('Resource id of the environment.')
output id string = managedEnvironment.id

@description('Name of the environment.')
output name string = managedEnvironment.name

@description('Default domain of the environment. External apps get <app>.<this domain>.')
output defaultDomain string = managedEnvironment.properties.defaultDomain
