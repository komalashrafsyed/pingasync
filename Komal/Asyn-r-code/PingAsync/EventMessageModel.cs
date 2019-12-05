using System;
using System.Collections.Generic;
using System.Text;
using System.Net.NetworkInformation;

namespace PingAsync
{
    public class EventMessageModel
    {
        public EventMessageModel(PingReply reply, string ipaddress, int ipid)
        {

            ipID = ipid;
            ipAddress = ipaddress;
            pingStatus = reply.Status + "";

            if (reply.Status == IPStatus.Success)
            {
                roundTripTime = reply.RoundtripTime;
                timetoLive = reply.Options.Ttl.ToString();
                msgFragment = reply.Options.DontFragment;
                bufferSize = reply.Buffer.Length;
            }


            //EventId = DateTime.Now.Day +  DateTime.Now.Minute;
            CorrelationId = Guid.NewGuid();
            EventTime = DateTime.Now;

        }


        public int ipID { get; set; }
        public string ipAddress { get; set; }
        public string pingStatus { get; set; }
        public long roundTripTime { get; set; }
        public string timetoLive { get; set; }

        public bool msgFragment { get; set; }
        public int bufferSize { get; set; }

        // public int EventId { get; set; }

        public Guid CorrelationId { get; set; }

        public DateTime EventTime { get; set; }


        //public string DisplayFormat => $"EventId: {EventId} | EventTime: {EventTime:HH:mm:ss} | CorrelationId: {CorrelationId} | Message: {Message}";

    }
}
