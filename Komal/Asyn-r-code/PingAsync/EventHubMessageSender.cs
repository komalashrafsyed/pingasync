using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace PingAsync
{

        public class EventHubMessageSender : IEventHubMessageSender
        {

            private readonly EventHubClient _eventHubClient;
            private const string PartitionKey = "counters";

            public EventHubMessageSender(EventHubConfiguration configuration)
            {
                var connectionStringBuilder = new EventHubsConnectionStringBuilder(configuration.ConnectionString)
                {
                    EntityPath = configuration.HubName

                };

                _eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
        }

            public async Task SendAsync(EventMessageModel message)
            {
                var eventData = new EventData(message.ToBytes());
                //await _eventHubClient.SendAsync(eventData, PartitionKey).ConfigureAwait(false);  // Partition key send events to a single partition only use when need ordered events
                await _eventHubClient.SendAsync(eventData).ConfigureAwait(false);
            }

            public async Task SendAsync(IEnumerable<EventMessageModel> messages)
            {
                var eventDataBatch = _eventHubClient.CreateBatch();
                var eventData = messages.ToEventData();

                foreach (var data in eventData)
                {
                    if (!eventDataBatch.TryAdd(data))
                    {
                        await SendBatchAsync(eventDataBatch);
                        eventDataBatch = _eventHubClient.CreateBatch();
                        eventDataBatch.TryAdd(data);
                    }
                }

                if (eventDataBatch.Count > 0)
                {
                    await SendBatchAsync(eventDataBatch);
                }

            }

            private async Task SendBatchAsync(EventDataBatch batch)
            {
                await _eventHubClient.SendAsync(batch).ConfigureAwait(false);
            }
        }
    
}
