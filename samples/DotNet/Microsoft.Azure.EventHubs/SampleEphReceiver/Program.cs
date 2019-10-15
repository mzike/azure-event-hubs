// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SampleEphReceiver
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;

    public class Program
    {
        private const string EventHubConnectionString = "Endpoint=sb://lcm-eh-ns-ae-dv.servicebus.windows.net/;SharedAccessKeyName=LcmListenAccessPolicy;SharedAccessKey=d82lE/9gomRmNzdY6oNZi4xMUKg6GzWrNAifRdocR84=";
        private const string EventHubName = "lcm-eh-ae-dv";
        private const string StorageContainerName = "blbephcontainer";
        private const string StorageAccountName = "zlcmehubchkpoint";
        private const string StorageAccountKey = "lIPUr5pslaM2rdb5rmfeKZVzG0QLY6NMz7Pkz81hoMyFBBE9B/5lI0AimuQJillCLVZC9s6WfRrbfLVAHdxugA==";

        private static readonly string StorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=zlcmehubchkpoint;AccountKey=Y6jby7NYyldZxRD5e6fhxjHR3A4JFoRTvxLMzn46k8d4c75AKG2N6Y8BsVxUXgYLDms2GgyUkAPR99gSrcT5yA==";

        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Registering EventProcessor...");

            var eventProcessorHost = new EventProcessorHost(
                EventHubName,
                PartitionReceiver.DefaultConsumerGroupName,
                EventHubConnectionString,
                StorageConnectionString,
                StorageContainerName);

            // Registers the Event Processor Host and starts receiving messages
            await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>();

            Console.WriteLine("Receiving. Press enter key to stop worker.");
            Console.ReadLine();

            // Disposes of the Event Processor Host
            await eventProcessorHost.UnregisterEventProcessorAsync();
        }
    }
}
