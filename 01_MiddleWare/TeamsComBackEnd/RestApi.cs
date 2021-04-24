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
                mLogger.LogError($"Could not insert item. Exception thrown: {ex.Message}");
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

        private async Task<string> GetCallDetails(string aCallId)
        {
            string lExit = "";
            try
            {
                String lDate = string.Empty;
                String lParticipants = string.Empty;
                QueryDefinition queryDefinition = new QueryDefinition("SELECT distinct top 10 t.who, t[\"when\"] FROM teamscalls t order by t._ts desc");

                using (FeedIterator<TranscriptionEntity> feedIterator = this.mContainer.GetItemQueryIterator<TranscriptionEntity>(queryDefinition,
                    null,
                    new QueryRequestOptions() { PartitionKey = new PartitionKey(aCallId) }))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        foreach (var item in await feedIterator.ReadNextAsync())
                        {
                            if (string.IsNullOrEmpty(lDate))
                            {
                                lDate = $"{item.When.ToUniversalTime()}";
                            }

                            if (string.IsNullOrEmpty(lParticipants))
                            {
                                lParticipants = $"{item.Who.Substring(0, item.Who.IndexOf('['))}";
                            }
                            else
                            {
                                string lParticipant = item.Who.Substring(0, item.Who.IndexOf('['));
                                if (!lParticipants.Contains(lParticipant))
                                {
                                    lParticipants += $", {lParticipant}";
                                }                                
                            }
                        }
                    }

                    lExit = $"[{lDate} UTC] [With: {lParticipants}...]";
                }

                
            }
            catch (Exception ex)
            {
                mLogger.LogError($"GetCallDetails failed. Exception thrown: {ex.Message}");                
            }

            return lExit;
        }
    }
}
