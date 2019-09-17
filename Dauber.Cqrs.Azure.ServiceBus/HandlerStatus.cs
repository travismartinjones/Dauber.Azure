using System;
using System.Collections.Concurrent;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class HandlerStatus
    {
        public class ExceptionCase
        {
            public string Message;
            public string Stack;
            public DateTime Date;
        }
        
        public class Status
        {
            public ConcurrentDictionary<int,int> MessagesPerSecond = new ConcurrentDictionary<int, int>();
            public ConcurrentDictionary<int,int> TimeoutsPerSecond = new ConcurrentDictionary<int, int>();
            public ConcurrentDictionary<int,int> ExceptionsPerSecond = new ConcurrentDictionary<int, int>();            
            public double AverageDurationSeconds = 0;
            public double AverageQueuedSeconds = 0;  
            public double AverageCompleteSeconds = 0;  
            public double AverageAbandonSeconds = 0;  
            public long TotalMessages;
            public long TotalCompleted;
            public long TotalAbandoned;
            public long TotalBusCompleted;
            public long TotalBusAbandoned;
            public long TotalErrors;
            public string SlowestId;
            public double SlowestSeconds;
            public DateTime SlowestTime;
            public ExceptionCase LastTimeout;
            public ExceptionCase LastException;
            public DateTime LastUpdated;
            public DateTime CreateDate = DateTime.UtcNow;            
            public string CompleteSlowestId;
            public double CompleteSlowestSeconds;
            public DateTime CompleteSlowestTime;
            public string AbandonSlowestId;
            public double AbandonSlowestSeconds;
            public DateTime AbandonSlowestTime;
            public long TotalSlowComplete;
            public long TotalSlowClose;
            public long TotalSlowAbandon;
        }

        public ConcurrentDictionary<string, Status> Statuses = new ConcurrentDictionary<string, Status>();
    }
}