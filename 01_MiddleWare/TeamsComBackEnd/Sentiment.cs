using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.ApplicationInsights.DataContracts;
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
            var lTimeStamp = DateTime.UtcNow;            
            bool lSuccess = false;

            try
            {
                var client = new TextAnalyticsClient(endpoint, credentials);

                DocumentSentiment documentSentiment = client.AnalyzeSentiment(aInput, "en-US");

                var lSentiment = documentSentiment.Sentences.ToList()[0].Sentiment.ToString();

                lSuccess = true;

                return lSentiment;
            }
            catch (Exception ex)
            {
                // This should be Track.exception on App Insights.
                log.LogError(ex, $"Failed GetSentiment. Details: {ex.Message}");
            }
            finally
            {
                var lDurartion = DateTime.UtcNow.Subtract(lTimeStamp);
                DependencyTelemetry lDep = new DependencyTelemetry("Sentiment Analysis", "Azure Cognitive Services", "TextAnalytics", "", lTimeStamp, lDurartion, "", lSuccess);
                MyAppInsights.Logger.TrackDependency(lDep);
            }

            return null;
        }
    }
}
