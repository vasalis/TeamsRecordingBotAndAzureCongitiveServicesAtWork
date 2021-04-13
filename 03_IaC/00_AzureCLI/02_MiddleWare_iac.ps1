# Helping variables
$projectPrefix = "myrecbot"
$resourceGroupName = $projectPrefix + "_rg"
$azureLocation = "northeurope"
Write-Output "(Got from ENV): RG: $resourceGroupName location: $azureLocation"
Write-Output "Environment Azure CL: $(az --version)"

# Cognitive Services Related Variables
$textAnalyticsName = $projectPrefix +"TextAnalytics"
$translationName = $projectPrefix +"Translation"

# Functions related variables
$functionsName = $projectPrefix + "middleware"
$storageAccountName = $projectPrefix + 'funstorage'
$functionsAppInsightsName = $functionsName + 'AI'

# Cosmos Related variables
$cosmosDbAccount = $projectPrefix + 'cosmosdb'
$cosmosDbName = 'TeamsCallsDatabase'
$cosmosDbContainerName = 'TeamsCalls'
$cosmosDbPartitionKey = '/callid'

# Create the resource group
Write-Output "About to create resource group: $resourceGroupName" 
az group create -l $azureLocation -n $resourceGroupName

# Create the Text Analytics and Translation Cognitive Service
Write-Output "About to create Text Analytics and Translation Cognitive Services: $textAnalyticsName, $translationName"
az cognitiveservices account create --name $textAnalyticsName --resource-group $resourceGroupName --kind TextAnalytics --sku S --location $azureLocation --yes
az cognitiveservices account create --name $translationName --resource-group $resourceGroupName --kind TextTranslation --sku S1 --location $azureLocation --yes
# Get keys and urls as variables
$textAnalyticsKey = az cognitiveservices account keys list --name $textAnalyticsName --resource-group $resourceGroupName --query 'key1'
$textAnalyticsEndPoint = "https://" + $textAnalyticsName +".cognitiveservices.azure.com/"
$translationsKey = az cognitiveservices account keys list --name $translationName --resource-group $resourceGroupName --query 'key1'

# Create the Cosmos Db
Write-Output "About to create Cosmos Db account: $cosmosDbAccount"
az cosmosdb create --name $cosmosDbAccount --resource-group $resourceGroupName

# Get Cosmos keys and pass them as Application variables
$cosmosPrimaryKey = az cosmosdb keys list --name $cosmosDbAccount --resource-group $resourceGroupName --type keys --query 'primaryMasterKey'
$cosmosConString = "AccountEndpoint=https://"+$cosmosDbAccount+".documents.azure.com:443/;AccountKey="+$cosmosPrimaryKey

# Create Application Insights for Functions
Write-Output "About to create Application Insights: $functionsAppInsightsName"
az extension add --name application-insights
az monitor app-insights component create -a $functionsAppInsightsName -l $azureLocation -g $resourceGroupName
$appInsightsKey = az monitor app-insights component show --app $functionsAppInsightsName -g $resourceGroupName --query 'instrumentationKey'
Write-Output "Got app insights key: $appInsightsKey"

# Create the storage account to be used for Functions
Write-Output "About to create Storage for Functions hosting: $storageAccountName"
az storage account create -n $storageAccountName -g $resourceGroupName -l $azureLocation --kind StorageV2

# Create new Azure Functions with .NetCore, Application Insights
# Linux is not working for consumption plan: https://github.com/Azure/azure-cli/pull/12817
# Hosted agents use Azure CLI 2.3.1
# az functionapp create -c $azureLocation -n $functionsName --os-type Linux -g $resourceGroupName --runtime dotnet -s $storageAccountName --app-insights $appInsightsName --app-insights-key $appInsightsKey
Write-Output "About to create Functions middleware: $functionsName"
az functionapp create -c $azureLocation -n $functionsName --os-type Windows -g $resourceGroupName --runtime dotnet -s $storageAccountName --app-insights $functionsAppInsightsName --app-insights-key $appInsightsKey

# Setup environment variables for Azure Function
az functionapp config appsettings set --name $functionsName --resource-group $resourceGroupName --settings "CosmosConnectionString=$cosmosConString"
az functionapp config appsettings set --name $functionsName --resource-group $resourceGroupName --settings "CosmosDbName=$cosmosDbName"
az functionapp config appsettings set --name $functionsName --resource-group $resourceGroupName --settings "CosmosDbContainerName=$cosmosDbContainerName"
az functionapp config appsettings set --name $functionsName --resource-group $resourceGroupName --settings "CosmosDbPartitionKey=$cosmosDbPartitionKey"
az functionapp config appsettings set --name $functionsName --resource-group $resourceGroupName --settings "TextAnalyticsKey=$textAnalyticsKey"
az functionapp config appsettings set --name $functionsName --resource-group $resourceGroupName --settings "TextAnalyticsEndPoint=$textAnalyticsEndPoint"