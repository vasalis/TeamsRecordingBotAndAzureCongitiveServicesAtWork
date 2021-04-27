using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace TeamsComBackEnd
{
    public class Sentiment
    {        
        private static readonly AzureKeyCredential credentials = new AzureKeyCredential(Environment.GetEnvironmentVariable("TextAnalyticsKey"));
        private static readonly Uri endpoint = new Uri(Environment.GetEnvironmentVariable("TextAnalyticsEndPoint"));

        public static string GetSentiment(string aInput, ILogger log)
        {
            try
            {
                var client = new TextAnalyticsClient(endpoint, credentials);

                DocumentSentiment documentSentiment = client.AnalyzeSentiment(aInput, "en-US");

                var lSentiment = documentSentiment.Sentences.ToList()[0].Sentiment.ToString();

                return lSentiment;
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed GetSentiment. Details: {ex.Message}");
            }

            return null;
        }
    }
}
