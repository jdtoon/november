type envVar = {
  @description('Variable name.')
  name: string

  @description('Plain value. Leave unset when secretRef is used.')
  value: string?

  @description('Name of an app secret. Leave unset when value is used.')
  secretRef: string?
}

type keyVaultSecret = {
  @description('Secret name inside the app.')
  name: string

  @description('Key Vault secret URL. Omit the version to follow the latest.')
  keyVaultUrl: string
}

@description('Name of the container app. Within an environment it doubles as the internal DNS name other apps use.')
param name string

@description('Azure region for the app.')
param location string

@description('Tags applied to the app.')
param tags object

@description('Resource id of the Container Apps environment.')
param environmentId string

@description('Resource id of the user-assigned identity used to pull the image and read Key Vault secrets.')
param identityId string

@description('Login server of the container registry, for example myregistry.azurecr.io.')
param registryServer string

@description('Full image reference, for example myregistry.azurecr.io/november-bff:abc123.')
param image string

@description('Port the container listens on.')
param targetPort int = 8080

@description('True exposes the app on the internet. False keeps it reachable only from inside the environment.')
param externalIngress bool = false

@description('CPU cores per replica, as a string such as 0.25. Must be a valid Consumption pairing with memory.')
param cpu string = '0.25'

@description('Memory per replica, for example 0.5Gi.')
param memory string = '0.5Gi'

@description('Minimum replicas. Zero lets the app scale to zero when idle.')
@minValue(0)
param minReplicas int = 0

@description('Maximum replicas.')
@minValue(1)
param maxReplicas int = 2

@description('Concurrent HTTP requests per replica that trigger a scale-out.')
@minValue(1)
param httpConcurrentRequests int = 50

@description('Path the liveness probe calls.')
param livenessPath string

@description('Path the readiness probe calls.')
param readinessPath string

@description('Environment variables. Use value for plain settings and secretRef for secrets.')
param env envVar[] = []

@description('Secrets resolved from Key Vault with the app identity.')
param keyVaultSecrets keyVaultSecret[] = []

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    environmentId: environmentId
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: externalIngress
        targetPort: targetPort
        transport: 'auto'
        allowInsecure: !externalIngress
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: registryServer
          identity: identityId
        }
      ]
      secrets: [
        for secret in keyVaultSecrets: {
          name: secret.name
          keyVaultUrl: secret.keyVaultUrl
          identity: identityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: name
          image: image
          env: env
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: livenessPath
                port: targetPort
              }
              initialDelaySeconds: 5
              periodSeconds: 10
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: readinessPath
                port: targetPort
              }
              initialDelaySeconds: 3
              periodSeconds: 5
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-concurrency'
            http: {
              metadata: {
                concurrentRequests: string(httpConcurrentRequests)
              }
            }
          }
        ]
      }
    }
  }
}

@description('Name of the container app.')
output name string = app.name

@description('Fully qualified domain name of the app ingress.')
output fqdn string = app.properties.configuration.ingress.fqdn

@description('URL of the app ingress. Only reachable from the internet when externalIngress is true.')
output url string = 'https://${app.properties.configuration.ingress.fqdn}'
