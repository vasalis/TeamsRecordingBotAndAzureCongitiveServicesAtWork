# [Work in progress] This is a git repo that tries to put together a Teams Recording Bot, Azure Cognitive Services and a Teams Tab app to demonstrate end to end functionality that:
1. Captures the audio from a Team's call (by adding the "Recording bot" as a user)
2. Transcribes the audio to the default language (english, but you can change that via code ;) ) 
3. Translates the transcription to one or more languages (again you can change these)
4. Shows the results on a Teams Tab App (React / TypeScript)
# Steps to get things running
1. [First start by registering a bot](https://github.com/microsoftgraph/microsoft-graph-comms-samples/blob/master/Samples/V1.0Samples/AksSamples/teams-recording-bot/docs/setup/bot.md)
   1. From this you will need BOT_ID, BOT_SECRET and BOT_NAME. The first two will need to be kept as GitHub [repo secrets](https://docs.github.com/en/actions/reference/encrypted-secrets#creating-encrypted-secrets-for-a-repository)
   2. Create a Secret on your repo named BOT_ID holding the value of BOT_ID
   3. Create a Secret on your repo named BOT_SECRET holding the value of BOT_SECRET
2. Change the values of 03_IaC\00_AzureCLI\MyDeploymentValues.txt with your desired values
   1. botSubDomain is the subdomain that your bot will "listen" to, for example myrecbot.mydomain.myextention
   2. botName is the value from step 1 BOT_NAME, use the same
   3. projectPrefix is a prefix, for naming conventions. Use something yours...
   4. azureLocation the Azure region to deploy things
   5. acrName the name of the Azure Container Registry to be created. This needs to be unique as its an FQDN
3. Once you makes changes to MyDeploymentValues.txt, pushing to remote repo will trigger the Initial Setup workflow. This will create:
   1. A Public IP. This is of paramount importance, as you will need to create an A record for your custom domain, mapping this IP to your botSubDomain.
   2. An Azure Container Registry. This will hold the Docker image (windows) of your Bot.
4. Once the above is completed, you should see two Issues being created. At this point you will need to do below two very important actions:
   1. Create an A record mapping the newly created IP to your bot's subdomain. To do this, you will need to visit your domain's registrant web site, and make the related DNS configurations. This should be quite straight forward. Once you complete it, try to ping your bot's subdomain to make sure that the Azure IP is resolved.
   2. Visit your Azure Container Registry and get a copy of the user name and the key of your [Admin Account](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-authentication#admin-account) this is supposed to be enabled by the Initial Setup workflow.
   3. Create a GitHub secret named REGISTRY_USERNAME holding the value of your ACR Admin Account user name
   4. Create a GitHub secret named REGISTRY_PASSWORD holding the value of your ACR Admin Account key.
3. Run manually the CI_CD_RecordingBot. This will build the Bot's code and create the first version of your Docker image in your ACR. This can be quite time consuming...expect 20mins+. 
4. Once you have completed the above you should have in place:
   1. IP mapped to your bot's subdomain
   2. Your Bot's code build into a Docker images stored on your ACR.
5. Run manually the Setup_infra workflow. This will create:
   1. This will create an AKS cluster with a Windows Pool
   2. Will move the IP created on the first step to MC_ resource group (automatically created by AKS creation). This is needed so this IP can be assigned to AKS's load balancer. (Probably there is an easier way to solve this, when using a SP dedicated for the creation of the components - TODO for now...) 
6. It is pretty much automating the steps described [here](https://github.com/vasalis/TeamsRecordingBotAndAzureCongitiveServicesAtWork/blob/master/00_RecordingBot/docs/deploy/aks.md)
7. After it's finished (again 15mins+) make sure your [run the validations](https://github.com/vasalis/TeamsRecordingBotAndAzureCongitiveServicesAtWork/blob/master/00_RecordingBot/docs/deploy/aks.md#validate-deployment) to see if everything is ok...In case it is not, please review the logs in order to determine possible issues.
   1. If everything is ok (well congrats) you should be able to add a bot to a Teams call. Add this point, you should already have registered your bot to your Teams tenant (grant admin consent on step 1).
   2. So, create a teams meeting, get the Join Url, and make the below POST Call
    ```html
    POST https://botSubDomain/joinCall
    Content-Type: application/json
    {
        "JoinURL": "https://teams.microsoft.com/l/meetup-join/...",
    }
    ```
   3. For more details on this see [here](https://github.com/microsoftgraph/microsoft-graph-comms-samples/tree/master/Samples/V1.0Samples/LocalMediaSamples/AudioVideoPlaybackBot#test)
# Fork
The Recording bot is a fork of this public repo: https://github.com/microsoftgraph/microsoft-graph-comms-samples/tree/master/Samples/V1.0Samples/AksSamples/teams-recording-bot
which follows the MIT license. This part is located [here](https://github.com/vasalis/TeamsRecordingBotAndAzureCongitiveServicesAtWork/tree/master/00_RecordingBot)

# Original Work
MiddleWare and Front end are original work in the context of a prove of concept.
# Environment
1. AKS cluster (with windows pool) to run the recording bot
2. Azure Functions (C#) as middleware between the bot and the front end (Teams Tab app)
3. Cosmos db as persistance layer

# Current state
Working on Infrastructure as code, in order to deploy everything automatically [see here](https://github.com/vasalis/TeamsRecordingBotAndAzureCongitiveServicesAtWork/tree/master/03_IaC/00_AzureCLI)

# Disclaimer
This is a work in progress, its just a proof of concept, not meant for production purpuses.
Time permitted, more detailed documentation to follow.