using System;
using System.Collections.Generic;
namespace SafeAccountsAPI.Logging
{
    public class LoggingInfo
    {
        public LoggingInfo()
        {
            TimeStamp = DateTime.Now;
        }

        public DateTime TimeStamp { get; private set; }
        public string Message { get; set; }

        public string Layer { get; set; }

        public string Location { get; set; }

        public string HostName { get; set; }

        public string UserID { get; set; }
        public long? ElapsedMilliseconds { get; set; }

        public Exception Exception { get; set; }

        public Guid CorrelationID { get; set; }

        public Dictionary<string, object> AdditionalInfo { get; set; }
    }
}