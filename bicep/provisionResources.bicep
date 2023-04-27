targetScope = 'resourceGroup'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Common name for all resources')
param commonResourceName string = 'GSNVSearchDemo'

var swaSku = 'Free'
var deadLetterQueueName = 'deadletter'
var functionAppName = '${commonResourceName}func'
var keyvaultName = '${commonResourceName}keyvault'
var serviceBusQueueNames = [
  'SearchDocument'
  'ListenerDemo'
]
var storageAccountName = '${commonResourceName}storage'
var storageBlobDataContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '17d1049b-9a84-46fb-8f53-869881c3d3ab')
var keyVaultSecretsUserRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: '${commonResourceName}servicebus'
  location: location
  sku: {
    name: 'Basic'
  }

  resource deadLetterQueue 'queues@2018-01-01-preview' = {
    name: deadLetterQueueName
    properties: {
      requiresDuplicateDetection: false
      requiresSession: false
      enablePartitioning: false
    }
  }

  resource queues 'queues@2018-01-01-preview' = [for queueName in serviceBusQueueNames: {
    name: queueName
    dependsOn: [
      deadLetterQueue
    ]
    properties: {
      forwardDeadLetteredMessagesTo: deadLetterQueueName
      lockDuration: 'PT1M'
      maxSizeInMegabytes: 1024
      requiresDuplicateDetection: false
      requiresSession: false
      defaultMessageTimeToLive: 'P10D'
      deadLetteringOnMessageExpiration: true
      duplicateDetectionHistoryTimeWindow: 'PT10M'
      maxDeliveryCount: 10
      enablePartitioning: false
      enableExpress: false
      enableBatchedOperations: true
      status: 'Active'
    }
  }]
}

resource searchService 'Microsoft.Search/searchServices@2020-08-01' = {
  name: '${commonResourceName}search'
  location: location
  sku: {
    name: 'standard'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

resource signalRService 'Microsoft.SignalRService/SignalR@2020-05-01' = {
  name: '${commonResourceName}signalr'
  location: location
  sku: {
    name: 'Free_F1'
    capacity: 1
  }
  properties: {
    cors: {
      allowedOrigins: [
        '*'
      ]
    }
  }
}

resource redisCache 'Microsoft.Cache/Redis@2020-06-01' = {
  name: '${commonResourceName}redis'
  location: location
  properties: {
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 1
    }
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    encryption: {
      keySource: 'Microsoft.Storage'
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
        queue: {
          enabled: true
        }
        table: {
          enabled: true
        }
      }
    }
  }
}

resource storageFunctionAppPermissions 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(storageAccount.id, functionAppName, storageBlobDataContributorRole)
  scope: storageAccount
  properties: {
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: storageBlobDataContributorRole
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: '${commonResourceName}log'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: 1
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${commonResourceName}insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: keyvaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForDiskEncryption: true
    enabledForTemplateDeployment: false
    enablePurgeProtection: false
    enableSoftDelete: false
    sku: {
      family: 'A'
      name: 'standard'
    }
  }

  resource storageAccountConnectionStringSecret 'secrets' = {
    name: 'storageAccountConnectionString'
    properties: {
      value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
    }
  }
  
  resource serviceBusConnectionStringSecret 'secrets' = {
    name: 'serviceBusConnectionStringString'
    properties: {
      value: listKeys('${serviceBusNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', serviceBusNamespace.apiVersion).primaryConnectionString
    }
  }
  
  resource searchServiceKey 'secrets' = {
    name: 'searchServiceKey'
    properties: {
      value: listAdminKeys('${searchService.name}', searchService.apiVersion).primaryKey
    }
  }

  resource signalrServiceConnectionStringSecret 'secrets' = {
    name: 'signalrServiceConnectionString'
    properties: {
      value: 'Endpoint=https://${signalRService.name}.service.signalr.net;AccessKey=${signalRService.listKeys().primaryKey}'
    }
  }
  
  resource redisCacheConnectionStringSecret 'secrets' = {
    name: 'redisCacheConnectionString'
    properties: {
      value: '${redisCache.name}.redis.cache.windows.net,abortConnect=false,ssl=true,password=${redisCache.listKeys().primaryKey}'    
    }
  }
}

resource keyVaultFunctionAppPermissions 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(keyVault.id, functionAppName, keyVaultSecretsUserRole)
  scope: keyVault
  properties: {
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRole
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: '${commonResourceName}asp'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
}

resource functionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v6.0'
    }
    httpsOnly: true
  }

  resource functionAppConfiguration 'config' = {
    name: 'appsettings'
    properties: {
      AzureWebJobsStorage: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${keyVault::storageAccountConnectionStringSecret.name})'
      APPINSIGHTS_INSTRUMENTATIONKEY: applicationInsights.properties.InstrumentationKey
      FUNCTIONS_WORKER_RUNTIME: 'dotnet'
      FUNCTIONS_EXTENSION_VERSION: '~4'
      ServiceBusConnectionString: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${keyVault::serviceBusConnectionStringSecret.name})'
      SearchServiceEndPoint: 'sb://${searchService.name}.servicebus.windows.net/'
      SearchAdminApiKey: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${keyVault::searchServiceKey.name})'
      IndexName: 'Addresses'
      SignalrServiceConnectionString: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${keyVault::signalrServiceConnectionStringSecret.name})'
      RedisCacheConnectionString: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${keyVault::redisCacheConnectionStringSecret.name})'
    }
    dependsOn: [
      storageFunctionAppPermissions
      keyVaultFunctionAppPermissions
    ]
  }  
}

resource staticWebApp1 'Microsoft.Web/staticSites@2020-12-01' = {
  name: '${commonResourceName}swa1'
  location: location
  sku: {
    name: swaSku
    size: swaSku
  }
  properties: {}

  resource staticWebAppAppSettings 'config' = {
    name: 'appsettings'
    properties: {
      APPINSIGHTS_INSTRUMENTATIONKEY: applicationInsights.properties.InstrumentationKey
      SearchServiceEndPoint: 'sb://${searchService.name}.servicebus.windows.net/'
      SearchAdminApiKey: listAdminKeys('${searchService.name}', searchService.apiVersion).primaryKey
      IndexName: 'Addresses'
    }
  }
}

resource staticWebApp2 'Microsoft.Web/staticSites@2020-12-01' = {
  name: '${commonResourceName}swa2'
  location: location
  sku: {
    name: swaSku
    size: swaSku
  }
  properties: {}

  resource staticWebAppAppSettings 'config' = {
    name: 'appsettings'
    properties: {
      APPINSIGHTS_INSTRUMENTATIONKEY: applicationInsights.properties.InstrumentationKey
      FunctionKey: listkeys('${functionApp.id}/host/default', '2016-08-01').functionKeys.default
      FunctionUrl: '${functionApp.properties.defaultHostName}/api'
    }
  }
}
