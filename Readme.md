#  Git repo that puts together a Teams (recording) Bot with real-time audio access, Azure Cognitive Services and a Teams Tab app to demonstrate end to end functionality that:
1. Captures the audio from a Team's call (by adding the "Recording bot" as a user)
2. Transcribes the audio to the default language (english, but you can change that via code ;) ) 
3. Translates the transcription to one or more languages (again you can change these)
4. Shows the results on a Teams Tab App (React / TypeScript)
6. The bot does not actually record the call, but the code for that is just commented out from the original forked project (see below).

## Prerequisites
1. An Azure subcription, where you can create (or have access) to a Service Principal with the Owner Role.
2. A Teams Tenant with Admin rights
3. A custom domain, where you can create an A Record

## TL;DR
1. The scope of this repo is by using GitHub Actions to automate the entire process of running the sample. 
2. In theory, by:
   1. Registering a calling bot (Step 1) and creating an Azure Service principal (Step 2)
   2. Forking the repo 
   3. Creating a file named `03_IaC\00_AzureCLI\MyDeploymentValues.txt` with your config values
   4. and following auto-created issues on your repo.
3. you should be set. 
4. Estimated time to complete ~1.5-2hours (mostly due to building the Bot's docker image and spawning the hosting infrastructure)

# Steps to get things running
1. First start by: [registering a calling bot](https://github.com/vasalis/TeamsRecordingBotAndAzureCongitiveServicesAtWork/tree/master/00_RecordingBot/docs/setup/bot.md)
   1. From this you will need BOT_ID, BOT_SECRET and BOT_NAME. The first two will need to be kept as [repo secrets](https://docs.github.com/en/actions/reference/encrypted-secrets#creating-encrypted-secrets-for-a-repository)
   2. Create a Secret on your GitHub repo named BOT_ID holding the value of BOT_ID
   3. Create a Secret on your GitHub repo named BOT_SECRET holding the value of BOT_SECRET
2. Create an Azure Service Principal
   1. Run this: `az ad sp create-for-rbac -n "RecBotGitHubActions" --role Owner --sdk-auth` (you can use any name you like instead of RecBotGitHubActions)
   2. Get the result and create a GitHub repo Secret named AZURE_CREDENTIALS. For more details see [here](https://github.com/marketplace/actions/azure-login)
   3. Your GitHub repo secrets should look like this: </br>
   <img src="https://raw.githubusercontent.com/vasalis/TeamsRecordingBotAndAzureCongitiveServicesAtWork/master/00_RecordingBot/docs/images/secrets.jpg" width="650px"/>
3. Rename (or create a new) the file `03_IaC\00_AzureCLI\MyDeploymentValues_template.txt` to `03_IaC\00_AzureCLI\MyDeploymentValues.txt` and set your desired values
   1. `botSubDomain` is the subdomain that your bot will "listen" to, for example myrecbot.mydomain.myextention
   2. `botName` is the value from step 1 BOT_NAME, use the same
   3. `projectPrefix` is a prefix, for naming conventions. This will create an Azure resource group named: <projectPrefix>_rg. Use something yours...
   4. `azureLocation` the Azure region to deploy things
   5. `acrName` the name of the Azure Container Registry to be created. This needs to be unique as its a FQDN
4. Go to `00_RecordingBot\deploy\cluster-issuer.yaml` edit the file and replace `YOUR_EMAIL` with your email. **Use your company (or domain) email**, as I am not sure if this will work with common providers (for example gmail.com, outlook.com etc etc)
5. Once you make changes to `MyDeploymentValues.txt` and `cluster-issuer.yaml`, pushing to remote repo will trigger the Initial Setup First Step workflow. This will create:
   1. A Public IP. This is of paramount importance, as you will need to create an A record for your custom domain, mapping this IP to your botSubDomain.
   2. An Azure Container Registry. This will hold the Docker image (windows) of your Bot.
6. Once the above is completed, you should see two Issues being created. At this point you will need to do below two very important actions:
   1. Create an A record mapping the newly created IP to your bot's subdomain. To do this, you will need to visit your domain's registrant web site, and make the related DNS configurations. This should be quite straight forward. Once you complete it, try to ping your bot's subdomain to make sure that the Azure IP is resolved.
   2. Visit your Azure Container Registry and get a copy of the user name and the key of your [Admin Account](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-authentication#admin-account) this is supposed to be enabled by the Initial Setup workflow.
   3. Create a GitHub secret named REGISTRY_USERNAME holding the value of your ACR Admin Account user name
   4. Create a GitHub secret named REGISTRY_PASSWORD holding the value of your ACR Admin Account key.
7. Run manually the Initial_Setup_Second_Step workflow. 
   1. This will build the Bot's code and create the first version of your Docker image in your ACR. This can be quite time consuming...expect 20mins+. 
   2. Next it will trigger the 02_Initial_Setup_All_Infra workflow, which will create an AKS cluster with a Windows Pool
   3. Will move the IP created on the first step to MC_ resource group (automatically created by AKS creation). This is needed so this IP can be assigned to AKS's load balancer. (Probably there is an easier way to solve this, when using a SP dedicated for the creation of the components - TODO for now...) 
   4. It will also deploy all needed things for the Middleware part (Azure Functions, Congitive Services, Cosmos db etc etc) 
   5. It is pretty much automating the steps described [here](https://github.com/vasalis/TeamsRecordingBotAndAzureCongitiveServicesAtWork/blob/master/00_RecordingBot/docs/deploy/aks.md)
8. After it's finished (total 45mins+) make sure you [run the validations](https://github.com/vasalis/TeamsRecordingBotAndAzureCongitiveServicesAtWork/blob/master/00_RecordingBot/docs/deploy/aks.md#validate-deployment) to see if everything is ok...In case it is not, please review the logs in order to determine possible issues.
   1. If everything is ok (well congrats :clap: :handshake:) you should be able to add a bot to a Teams call. Add this point, you should already have registered your bot to your Teams tenant (grant admin consent on step 1).
   2. So, create a teams meeting, get the Join Url, and make the below POST Call
    ```html
    POST https://botSubDomain/joinCall
    Content-Type: application/json
    {
        "JoinURL": "https://teams.microsoft.com/l/meetup-join/...",
    }
    ```
   3. For more details on this see [here](https://github.com/microsoftgraph/microsoft-graph-comms-samples/tree/master/Samples/V1.0Samples/LocalMediaSamples/AudioVideoPlaybackBot#test)
9. Teams Tab App
   1. Front End CI CD GitHub Action will deploy a Teams Tab App to an Azure Storage Static site on your newly created resource group
   2. Also, it create the Team's app package that you can deploy on Teams. The app package will be hosted on https://<Project Prefix>staticsite.z16.web.core.windows.net/teams-cognitive-bot.zip
   3. You can download the app package and install it on your Teams client using [Team's App Studio](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/build-and-test/app-studio-overview)
   4. For this to work, and start showing the transcriptions and translations, you would need the Call Id that is stored on Cosmos db.
10. Putting it all together
   1. Once you have your bot added as a user to your Teams call
   2. Your Teams tab app installed
   3. Go to your Cosmos db created, open data explorer and find your call id
   4. Paste that on your teams tab...this should start showing the transcripions and translations...
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