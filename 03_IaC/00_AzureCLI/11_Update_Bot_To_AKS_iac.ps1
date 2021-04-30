# Helping variables
$botSubDomain = $env:botSubDomain
$azureLocation = $env:azureLocation
$projectPrefix = $env:projectPrefix
$resourceGroupName = $projectPrefix +"_rg"
$imgVersion = $env:imgVersion

$AKSClusterName = "recBotAKSCluster"

$AKSmgResourceGroup = "MC_"+$resourceGroupName+"_"+"$AKSClusterName"+"_"+$azureLocation
$publicIpName = "myRecBotPublicIP"

$acrName = $env:acrName

Write-Output "(Got from ENV): About to deploy for image tag: $imgVersion"
Write-Output "(Got from ENV): RG: $resourceGroupName, MC rg: $AKSmgResourceGroup, location: $azureLocation"
Write-Output "Environment Azure CL: $(az --version)"

# Get Public IP
$publicIpAddress = az network public-ip show --resource-group $AKSmgResourceGroup --name $publicIpName --query 'ipAddress'

# Connect to Cluster
Write-Output "Getting AKS credentials for cluster: $AKSClusterName"
az aks get-credentials --resource-group $resourceGroupName --name $AKSClusterName

# Setup Helm for recording bot
Write-Output "Setting up helm for teams-recording-bot for bot domain: $botSubDomain and Public IP: $publicIpAddress"
Write-Output "Image Version is: $imgVersion"
Write-Output "Make sure there is an A record for this...mapping your bot subdomain with your Public IP"

# Update Bot
helm upgrade teams-recording-bot 00_RecordingBot/deploy/teams-recording-bot --namespace teams-recording-bot --set host=$botSubDomain --set public.ip=$publicIpAddress --set image.domain="$acrName.azurecr.io" --set image.tag=$imgVersion