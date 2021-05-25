using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using TeamsComModels;
using System.Net.Http;
using System.Text;

namespace TeamsComBackEnd
{
    public class RestApi
    {
        private readonly ILogger mLogger;
        private readonly IConfiguration mConfig;
        private Container mContainer;

        public RestApi(ILogger<RestApi> logger,
            IConfiguration config,
            Container aDbContainer)
        {
            mLogger = logger;
            mConfig = config;
            mContainer = aDbContainer;
        }

        [FunctionName("StoreTranscription")]
        public async Task<IActionResult> StoreTranscription(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("StoreTranscription trigger function processed a request.");

            IActionResult returnValue = null;

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                var input = JsonConvert.DeserializeObject<TranscriptionEntity>(requestBody);

                // Try to Get Sentiment
                string lSentiment = Sentiment.GetSentiment(input.Text, log);

                if (!string.IsNullOrEmpty(lSentiment))
                {
                    input.Who = $"{input.Who} [{lSentiment}]";
                }

                if (!string.IsNullOrWhiteSpace(input.Text))
                {
                    ItemResponse<TranscriptionEntity> lUpdatedItem = await mContainer.UpsertItemAsync(input);                    

                    mLogger.LogInformation("Item updated or inserted");
                    mLogger.LogInformation($"This query cost: {lUpdatedItem.RequestCharge} RU/s");
                }
                
                returnValue = new OkObjectResult(input);
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Could not insert item. Exception thrown: {ex.Message}");
                returnValue = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return returnValue;
        }

        [FunctionName("GetTranscriptions")]
        public async Task<IActionResult> GetTranscriptions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetTranscriptions trigger function processed a request.");

            IActionResult returnValue = null;

            try
            {
                List<TranscriptionEntity> lResults = new List<TranscriptionEntity>();

                string lCallId = await new StreamReader(req.Body).ReadToEndAsync();

                QueryDefinition queryDefinition = new QueryDefinition("select TOP 20 * from TeamsCalls t order by t._ts desc");

                using (FeedIterator<TranscriptionEntity> feedIterator = this.mContainer.GetItemQueryIterator<TranscriptionEntity>(
                    queryDefinition,
                    null,
                    new QueryRequestOptions() { PartitionKey = new PartitionKey(lCallId) }))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        foreach (var item in await feedIterator.ReadNextAsync())
                        {
                            lResults.Add(item);
                        }
                    }

                    mLogger.LogInformation($"Got {lResults.Count} for callid: {lCallId}");

                    returnValue = new OkObjectResult(lResults);
                }


            }
            catch (Exception ex)
            {
                mLogger.LogError($"Could not GetTranscriptions. Exception thrown: {ex.Message}");
                returnValue = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return returnValue;
        }

        [FunctionName("GetActiveCalls")]
        public async Task<IActionResult> GetActiveCalls(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetActiveCalls trigger function processed a request.");

            IActionResult returnValue = null;

            try
            {
                List<CallEntity> lResults = new List<CallEntity>();                

                QueryDefinition queryDefinition = new QueryDefinition("SELECT distinct top 5 t.callid FROM teamscalls t order by t._ts desc");

                using (FeedIterator<CallEntity> feedIterator = this.mContainer.GetItemQueryIterator<CallEntity>(queryDefinition))
                {                    
                    while (feedIterator.HasMoreResults)
                    {
                        foreach (var item in await feedIterator.ReadNextAsync())
                        {
                            item.Text = await GetCallDetails(item.CallId);
                            lResults.Add(item);
                        }
                    }

                    mLogger.LogInformation($"Got {lResults.Count} top 5 calls");

                    returnValue = new OkObjectResult(lResults);
                }


            }
            catch (Exception ex)
            {
                mLogger.LogError($"Could not GetActiveCalls. Exception thrown: {ex.Message}");
                returnValue = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return returnValue;
        }

        [FunctionName("InviteBot")]
        public async Task<IActionResult> InviteBot(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("InviteBot trigger function processed a request.");

            IActionResult returnValue = null;

            try
            {
                string lBody = await new StreamReader(req.Body).ReadToEndAsync();

                HttpClient lhttp = new HttpClient();

                var lExit = await lhttp.PostAsync("https://cogbot.vsalis.eu/joinCall", new StringContent(lBody, Encoding.UTF8, "application/json"));

                string lresponseContent = await lExit.Content.ReadAsStringAsync();

                returnValue = new OkObjectResult(lresponseContent);
            }
            catch (Exception ex)
            {
                mLogger.LogError($"Could not InviteBot. Exception thrown: {ex.Message}");
                returnValue = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return returnValue;
        }

        private async Task<string> GetCallDetails(string aCallId)
        {
            string lExit = "";
            try
            {
                String lDate = string.Empty;
                String lParticipants = string.Empty;
                QueryDefinition queryDefinition = new QueryDefinition("SELECT top 1 t[\"when\"] FROM teamscalls t order by t._ts desc");

                using (FeedIterator<TranscriptionEntity> feedIterator = this.mContainer.GetItemQueryIterator<TranscriptionEntity>(queryDefinition,
                    null,
                    new QueryRequestOptions() { PartitionKey = new PartitionKey(aCallId) }))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        foreach (var item in await feedIterator.ReadNextAsync())
                        {
                            lDate = $"{item.When.ToUniversalTime()}";                            
                        }
                    }                    
                }

                queryDefinition = new QueryDefinition("SELECT distinct top 5 t.who FROM teamscalls t");

                using (FeedIterator<TranscriptionEntity> feedIterator = this.mContainer.GetItemQueryIterator<TranscriptionEntity>(queryDefinition,
                    null,
                    new QueryRequestOptions() { PartitionKey = new PartitionKey(aCallId) }))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        foreach (var item in await feedIterator.ReadNextAsync())
                        {
                            if (!string.IsNullOrEmpty(item.Who) && item.Who.Contains('['))
                            {
                                item.Who = item.Who.Substring(0, item.Who.IndexOf('['));
                            }

                            if (string.IsNullOrEmpty(lParticipants))
                            {
                                lParticipants = item.Who;
                            }
                            else
                            {
                                string lParticipant = item.Who;
                                if (!lParticipants.Contains(lParticipant))
                                {
                                    lParticipants += $", {lParticipant}";
                                }
                            }
                        }
                    }                    
                }

                lExit = $"[{lDate} UTC, with: {lParticipants}]";

            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"GetCallDetails failed. Exception thrown: {ex.Message}");                
            }

            return lExit;
        }
    }
}
