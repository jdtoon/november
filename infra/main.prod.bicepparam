using './main.bicep'

param environmentName = 'prod'
param imageTag = 'latest'
param acrSku = 'Premium'

param enableKeyVaultPurgeProtection = true
param keyVaultSoftDeleteRetentionInDays = 90

param logRetentionInDays = 90
param logDailyQuotaGb = -1

param shellScale = {
  minReplicas: 2
  maxReplicas: 10
}

param bffScale = {
  minReplicas: 2
  maxReplicas: 10
}

param remotesScale = {
  minReplicas: 1
  maxReplicas: 5
}

param tags = {
  purpose: 'reference-demo'
}
