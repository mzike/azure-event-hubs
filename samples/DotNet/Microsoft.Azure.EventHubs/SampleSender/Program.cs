﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;

namespace SampleSender
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    public class Program
    {
        private static EventHubClient eventHubClient;
        private static bool SetRandomPartitionKey = false;
       
        public static void Main(string[] args)
        {
          MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            var settings = new AppSettings();

            configuration.Bind(settings);

            IServiceCollection services = new ServiceCollection();

            services.AddApplicationInsightsTelemetryWorkerService(settings.ApplicationInsightsInstrumentationKey);

            // Build ServiceProvider.
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Obtain logger instance from DI.
            ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

          
            // Obtain TelemetryClient instance from DI, for additional manual tracking or to flush.
            var telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();

            // Creates an EventHubsConnectionStringBuilder object from a the connection string, and sets the EntityPath.
            // Typically the connection string should have the Entity Path in it, but for the sake of this simple scenario
            // we are using the connection string from the namespace.
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(settings.EventHubConnectionString)
            {
                EntityPath = settings.EventHubName
            };

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            logger.LogInformation("Sender running at: {time}, about to send messages to Event Hub.", DateTimeOffset.Now);

            await SendMessagesToEventHub(1000, logger, telemetryClient);

            await eventHubClient.CloseAsync();

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }

        // Creates an Event Hub client and sends 100 messages to the event hub.
        private static async Task SendMessagesToEventHub(int numMessagesToSend, ILogger<Program> logger, TelemetryClient telemetryClient)
        {
            var rnd = new Random();

            using (telemetryClient.StartOperation<RequestTelemetry>("Starting send operation"))
            {
                for (var i = 0; i < numMessagesToSend; i++)
                {
                    try
                    {
                        var message = $"Message {i}";

                        logger.LogInformation("Sending message {index} of {total}.", i.ToString(), numMessagesToSend.ToString());

                        // Set random partition key?
                        if (SetRandomPartitionKey)
                        {
                            var pKey = Guid.NewGuid().ToString();
                            await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)), pKey);
                            Console.WriteLine($"Sent message: '{message}' Partition Key: '{pKey}'");
                                
                        }
                        else
                        {
                            await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)));
                            Console.WriteLine($"Sent message: '{message}'");
                            telemetryClient.TrackEvent($"Message {i} call event completed.");
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"{DateTime.Now} > Exception: {exception.Message}");
                    }

                    await Task.Delay(10);
                }
            }

            Console.WriteLine($"{numMessagesToSend} messages sent.");
            telemetryClient.Flush();
        }
    }
}
