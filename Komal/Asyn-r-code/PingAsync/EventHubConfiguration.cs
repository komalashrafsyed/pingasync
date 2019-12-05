using System;
using System.Collections.Generic;
using System.Text;

namespace PingAsync
{
    public class EventHubConfiguration
    {
        public EventHubConfiguration(string connectionString, string hubName)
        {
            ConnectionString = connectionString;
            HubName = hubName;
     
        }

        public string ConnectionString { get; }

        public string HubName { get; }


    }
}
