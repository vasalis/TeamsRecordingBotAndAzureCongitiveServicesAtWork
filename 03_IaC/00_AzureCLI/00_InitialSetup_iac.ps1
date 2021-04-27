# TL;DR
# Run this first, 
# get the generated Public IP and then
# create an A record mapping the newly created IP to your bot's sub-domain (for example myrecbot.mydomain.myextention).

# This creates a Public IP on the MC_ resource group that will be created by the AKS creation.
# This Public IP needs to be mapped with the domain that the bot will use via an A record.
# This is needed to be setup before the creation the the Certification management of the AKS cluster.
# As certification management is expecting a specific public ip to be mapped to a specific subdomain in order to create a valid certificate.

# Helping variables
$botSubDomain = $env:botSubDomain
$azureLocation = $env:azureLocation
$projectPrefix = $env:projectPrefix
$resourceGroupName = $projectPrefix +"_rg"
$acrName = $env:acrName
$publicIpName = "myRecBotPublicIP"

Write-Output "(Got from ENV): RG: $resourceGroupName, location: $azureLocation"
Write-Output "Environment Azure CL: $(az --version)"

# Create the resource group
Write-Output "About to create resource group: $resourceGroupName" 
az group create -l $azureLocation -n $resourceGroupName

# Create the Azure Container Registry to hold the bot's docker image
Write-Output "About to create ACR: $acrName"
az acr create --resource-group $resourceGroupName --name $acrName --sku Basic --admin-enabled true

# Create a Public Ip
Write-Output "About to create public ip: $publicIpName"
az network public-ip create --resource-group $resourceGroupName --name $publicIpName --sku Standard --allocation-method static --zone 1
$publicIpAddress = az network public-ip show --resource-group $resourceGroupName --name $publicIpName --query 'ipAddress'
Write-Output "Got public ip: $publicIpAddress"
Write-Output "CREATED_IP=$publicIpAddress" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append

Write-Output "Now create an A record on your Domain registrant mapping: $botSubDomain to: $publicIpAddress"
Write-Output "Then run the rest of the IaC..."