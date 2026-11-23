using './main.bicep'

param environmentName = 'dev'
param imageTag = 'latest'
param acrSku = 'Basic'

param enableKeyVaultPurgeProtection = false
param keyVaultSoftDeleteRetentionInDays = 7

param logRetentionInDays = 30
param logDailyQuotaGb = 1

param shellScale = {
  minReplicas: 0
  maxReplicas: 2
}

param bffScale = {
  minReplicas: 0
  maxReplicas: 2
}

param remotesScale = {
  minReplicas: 0
  maxReplicas: 2
}

param tags = {
  purpose: 'reference-demo'
}
