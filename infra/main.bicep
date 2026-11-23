targetScope = 'resourceGroup'

type scaleSettings = {
  @description('Minimum replicas. Zero lets the app scale to zero when idle.')
  @minValue(0)
  minReplicas: int

  @description('Maximum replicas the HTTP scale rule can reach.')
  @minValue(1)
  maxReplicas: int
}

@description('Short environment name. Used in resource names and tags.')
@allowed([
  'dev'
  'prod'
])
param environmentName string

@description('Azure region. Defaults to the region of the resource group.')
param location string = resourceGroup().location

@description('Workload name used as the middle part of every resource name. Kept short because Key Vault names are limited to 24 characters.')
@minLength(3)
@maxLength(8)
param workloadName string = 'november'

@description('Extra tags merged over the default tags.')
param tags object = {}

@description('Image tag for all five container images. The deploy workflow passes the commit SHA.')
param imageTag string = 'latest'

@description('Set to false on the first run to create only the registry and supporting services. Push images, then deploy again with true. Without this the container apps cannot pull an image that does not exist yet.')
param deployApps bool = true

@description('Container registry SKU.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param acrSku string = 'Basic'

@description('Turn Key Vault purge protection on. Irreversible once enabled.')
param enableKeyVaultPurgeProtection bool

@description('Days a deleted Key Vault object stays recoverable.')
@minValue(7)
@maxValue(90)
param keyVaultSoftDeleteRetentionInDays int = 90

@description('Days of log retention in Log Analytics.')
@minValue(30)
@maxValue(730)
param logRetentionInDays int = 30

@description('Daily Log Analytics ingestion cap in GB. -1 means no cap.')
param logDailyQuotaGb int = -1

@description('Scale settings for the shell, the only public app.')
param shellScale scaleSettings

@description('Scale settings for the BFF.')
param bffScale scaleSettings

@description('Scale settings for the payments, approvals and incoming remotes.')
param remotesScale scaleSettings

var uniqueSuffix = take(uniqueString(resourceGroup().id, workloadName, environmentName), 6)
var baseName = '${workloadName}-${environmentName}-${uniqueSuffix}'
var defaultTags = {
  workload: workloadName
  environment: environmentName
  'managed-by': 'bicep'
}
var allTags = union(defaultTags, tags)

var appNames = {
  shell: 'ca-shell-${environmentName}'
  bff: 'ca-bff-${environmentName}'
  payments: 'ca-payments-${environmentName}'
  approvals: 'ca-approvals-${environmentName}'
  incoming: 'ca-incoming-${environmentName}'
}

var appInsightsSecretName = 'appinsights-connection-string'

module identity 'modules/identity.bicep' = {
  name: 'identity'
  params: {
    name: 'id-${baseName}'
    location: location
    tags: allTags
  }
}

module registry 'modules/containerRegistry.bicep' = {
  name: 'registry'
  params: {
    name: 'cr${workloadName}${environmentName}${uniqueSuffix}'
    location: location
    tags: allTags
    sku: acrSku
    pullerPrincipalId: identity.outputs.principalId
  }
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    logAnalyticsName: 'log-${baseName}'
    appInsightsName: 'appi-${baseName}'
    location: location
    tags: allTags
    retentionInDays: logRetentionInDays
    dailyQuotaGb: logDailyQuotaGb
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    name: 'kv-${baseName}'
    location: location
    tags: allTags
    vaultReaderPrincipalId: identity.outputs.principalId
    enablePurgeProtection: enableKeyVaultPurgeProtection
    softDeleteRetentionInDays: keyVaultSoftDeleteRetentionInDays
    appInsightsSecretName: appInsightsSecretName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}

module containerEnvironment 'modules/containerAppsEnvironment.bicep' = {
  name: 'containerAppsEnvironment'
  params: {
    name: 'cae-${baseName}'
    location: location
    tags: allTags
    logAnalyticsWorkspaceName: monitoring.outputs.workspaceName
  }
}

module bff 'modules/containerApp.bicep' = if (deployApps) {
  name: 'app-bff'
  params: {
    name: appNames.bff
    location: location
    tags: allTags
    environmentId: containerEnvironment.outputs.id
    identityId: identity.outputs.id
    registryServer: registry.outputs.loginServer
    image: '${registry.outputs.loginServer}/november-bff:${imageTag}'
    cpu: '0.5'
    memory: '1Gi'
    minReplicas: bffScale.minReplicas
    maxReplicas: bffScale.maxReplicas
    livenessPath: '/health/live'
    readinessPath: '/health/ready'
    env: [
      {
        name: 'Auth__Mode'
        value: 'Mock'
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        secretRef: appInsightsSecretName
      }
    ]
    keyVaultSecrets: [
      {
        name: appInsightsSecretName
        keyVaultUrl: keyVault.outputs.appInsightsSecretUrl
      }
    ]
  }
}

module payments 'modules/containerApp.bicep' = if (deployApps) {
  name: 'app-payments'
  params: {
    name: appNames.payments
    location: location
    tags: allTags
    environmentId: containerEnvironment.outputs.id
    identityId: identity.outputs.id
    registryServer: registry.outputs.loginServer
    image: '${registry.outputs.loginServer}/november-payments:${imageTag}'
    minReplicas: remotesScale.minReplicas
    maxReplicas: remotesScale.maxReplicas
    livenessPath: '/healthz'
    readinessPath: '/healthz'
  }
}

module approvals 'modules/containerApp.bicep' = if (deployApps) {
  name: 'app-approvals'
  params: {
    name: appNames.approvals
    location: location
    tags: allTags
    environmentId: containerEnvironment.outputs.id
    identityId: identity.outputs.id
    registryServer: registry.outputs.loginServer
    image: '${registry.outputs.loginServer}/november-approvals:${imageTag}'
    minReplicas: remotesScale.minReplicas
    maxReplicas: remotesScale.maxReplicas
    livenessPath: '/healthz'
    readinessPath: '/healthz'
  }
}

module incoming 'modules/containerApp.bicep' = if (deployApps) {
  name: 'app-incoming'
  params: {
    name: appNames.incoming
    location: location
    tags: allTags
    environmentId: containerEnvironment.outputs.id
    identityId: identity.outputs.id
    registryServer: registry.outputs.loginServer
    image: '${registry.outputs.loginServer}/november-incoming:${imageTag}'
    minReplicas: remotesScale.minReplicas
    maxReplicas: remotesScale.maxReplicas
    livenessPath: '/healthz'
    readinessPath: '/healthz'
  }
}

module shell 'modules/containerApp.bicep' = if (deployApps) {
  name: 'app-shell'
  params: {
    name: appNames.shell
    location: location
    tags: allTags
    environmentId: containerEnvironment.outputs.id
    identityId: identity.outputs.id
    registryServer: registry.outputs.loginServer
    image: '${registry.outputs.loginServer}/november-shell:${imageTag}'
    externalIngress: true
    minReplicas: shellScale.minReplicas
    maxReplicas: shellScale.maxReplicas
    livenessPath: '/healthz'
    readinessPath: '/healthz'
    env: [
      {
        name: 'BFF_UPSTREAM'
        value: 'http://${bff.?outputs.name ?? appNames.bff}'
      }
      {
        name: 'PAYMENTS_UPSTREAM'
        value: 'http://${payments.?outputs.name ?? appNames.payments}'
      }
      {
        name: 'APPROVALS_UPSTREAM'
        value: 'http://${approvals.?outputs.name ?? appNames.approvals}'
      }
      {
        name: 'INCOMING_UPSTREAM'
        value: 'http://${incoming.?outputs.name ?? appNames.incoming}'
      }
    ]
  }
}

@description('Public URL of the shell. Empty until the apps have been deployed.')
output shellUrl string = shell.?outputs.url ?? ''

@description('Container registry login server. The deploy workflow pushes images here.')
output acrLoginServer string = registry.outputs.loginServer

@description('Container registry name, for az acr login.')
output acrName string = registry.outputs.name

@description('Key Vault name.')
output keyVaultName string = keyVault.outputs.name

@description('Container Apps environment default domain.')
output environmentDefaultDomain string = containerEnvironment.outputs.defaultDomain
