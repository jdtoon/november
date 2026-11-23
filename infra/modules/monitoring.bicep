@description('Name of the Log Analytics workspace.')
param logAnalyticsName string

@description('Name of the Application Insights component.')
param appInsightsName string

@description('Azure region for both resources.')
param location string

@description('Tags applied to both resources.')
param tags object

@description('Days to keep logs in the workspace.')
@minValue(30)
@maxValue(730)
param retentionInDays int = 30

@description('Daily ingestion cap in GB. Use -1 for no cap. A cap protects a demo environment from a surprise bill.')
param dailyQuotaGb int = -1

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionInDays
    workspaceCapping: {
      dailyQuotaGb: dailyQuotaGb
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
    IngestionMode: 'LogAnalytics'
  }
}

@description('Resource id of the Log Analytics workspace.')
output workspaceId string = workspace.id

@description('Name of the Log Analytics workspace.')
output workspaceName string = workspace.name

@description('Application Insights connection string. Stored in Key Vault by the caller and never handed to apps directly.')
output appInsightsConnectionString string = appInsights.properties.ConnectionString
