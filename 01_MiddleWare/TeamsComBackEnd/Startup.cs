﻿using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(TeamsComBackEnd.Startup))]
namespace TeamsComBackEnd
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddFilter(level => true);
            });

            builder.Services.AddSingleton<Container>(GetContainer);
            
        }

        private Container GetContainer(IServiceProvider options)
        {
            try
            {
                var lConnectionString = Environment.GetEnvironmentVariable("CosmosConnectionString");
                var lCosmosDbName = Environment.GetEnvironmentVariable("CosmosDbName");
                var lCosmosDbContainerName = Environment.GetEnvironmentVariable("CosmosDbContainerName");
                var lCosmosDbPartionKey = Environment.GetEnvironmentVariable("CosmosDbPartitionKey");

                var lClient = new CosmosClient(lConnectionString, new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct
                });

                // Autoscale throughput settings
                // Set autoscale max RU/s
                ThroughputProperties lAutoscaleThroughputProperties = ThroughputProperties.CreateAutoscaleThroughput(4000);

                //Create the database with autoscale enabled
                lClient.CreateDatabaseIfNotExistsAsync(lCosmosDbName, throughputProperties: lAutoscaleThroughputProperties).Wait();
                var lDb = lClient.GetDatabase(lCosmosDbName);

                ContainerProperties lAutoscaleContainerProperties = new ContainerProperties(lCosmosDbContainerName, lCosmosDbPartionKey);
                lDb.CreateContainerIfNotExistsAsync(lAutoscaleContainerProperties, lAutoscaleThroughputProperties);

                return lDb.GetContainer(lCosmosDbContainerName);
            }
            catch (Exception ex)
            {
                var config = new TelemetryConfiguration(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));
                var client = new TelemetryClient(config);
                client.TrackException(ex);
                client.Flush();

                throw;
            }            
        }
    }
}
