using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using Dauber.Core.Time;
using HighIronRanch.Azure.ServiceBus.Contracts;
using DateTime = System.DateTime;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class CsvHandlerStatusProcessor : ICsvHandlerStatusProcessor
    {
        private readonly IDateTime _dateTime;
        HandlerStatus _statuses = new HandlerStatus();

        public CsvHandlerStatusProcessor(IDateTime dateTime)
        {
            _dateTime = dateTime;
        }

        public void Begin(string handlerType, string id, DateTime queuedDate)
        {            
            var status = GetStatus(handlerType);

            var now = _dateTime.UtcNow;

            var seconds = (int) (now - now.Date).TotalSeconds;
            status.MessagesPerSecond.AddOrUpdate(seconds, i => 1, (i, value) => value + 1);
            
            status.AverageQueuedSeconds = (status.AverageQueuedSeconds * status.TotalMessages + (now - queuedDate).TotalSeconds) / (status.TotalMessages + 1);                
            status.LastUpdated = now;
            Interlocked.Increment(ref status.TotalMessages);            
        }

        private void Cleanup(HandlerStatus.Status status)
        {       
            // keep only the last 60 distinct exception counts
            var keys = status.ExceptionsPerSecond.Keys;
            var keysToRemove = keys.OrderBy(x => x).Take(Math.Max(keys.Count - 60,0));
            foreach (var keyToRemove in keysToRemove)
                status.ExceptionsPerSecond.TryRemove(keyToRemove, out var temp);
            
            // keep only the last 60 distinct timeout counts
            keys = status.TimeoutsPerSecond.Keys;
            keysToRemove = keys.OrderBy(x => x).Take(Math.Max(keys.Count - 60,0));
            foreach (var keyToRemove in keysToRemove)
                status.TimeoutsPerSecond.TryRemove(keyToRemove, out var temp);

            // keep only the last 60 distinct timeout counts
            keys = status.MessagesPerSecond.Keys;
            keysToRemove = keys.OrderBy(x => x).Take(Math.Max(keys.Count - 60,0));
            foreach (var keyToRemove in keysToRemove)
                status.MessagesPerSecond.TryRemove(keyToRemove, out var temp);
        }

        public void Complete(string handlerType, string id, double elapsedSeconds)
        {            
            var status = GetStatus(handlerType);
            
            var now = _dateTime.UtcNow;
                        
            // update the slowest even handled withing a window of 30 minutes
            if (status.SlowestSeconds < elapsedSeconds || (now-status.SlowestTime).TotalMinutes > 30)
            {
                status.SlowestSeconds = elapsedSeconds;
                status.SlowestId = id;
                status.SlowestTime = now;
            }
            
            status.AverageDurationSeconds = (status.AverageDurationSeconds * status.TotalMessages + elapsedSeconds)/(status.TotalMessages+1);
            Interlocked.Increment(ref status.TotalCompleted);

            Cleanup(status);
        }

        private HandlerStatus.Status GetStatus(string handlerType)
        {
            var status = _statuses.Statuses.GetOrAdd(handlerType, s => new HandlerStatus.Status
            {
                AverageDurationSeconds = 0,
                AverageQueuedSeconds = 0,
                MessagesPerSecond = new ConcurrentDictionary<int, int>(),
                ExceptionsPerSecond = new ConcurrentDictionary<int, int>(),
                TimeoutsPerSecond = new ConcurrentDictionary<int, int>(),
                LastException = null,
                LastTimeout = null
            });
            return status;
        }

        public void BusComplete(string handlerType, string id, double elapsedSeconds)
        {
            var status = GetStatus(handlerType);
            status.AverageCompleteSeconds = (status.AverageCompleteSeconds * status.TotalBusCompleted + elapsedSeconds)/(status.TotalBusCompleted+1);
            Interlocked.Increment(ref status.TotalBusCompleted);

            var now = _dateTime.UtcNow;
            if (status.CompleteSlowestSeconds < elapsedSeconds || (now-status.CompleteSlowestTime).TotalMinutes > 30)
            {
                status.CompleteSlowestSeconds = elapsedSeconds;
                status.CompleteSlowestId = id;
                status.CompleteSlowestTime = now;
            }

            if (elapsedSeconds > 5)
                Interlocked.Increment(ref status.TotalSlowComplete);
        }

        public void BusAbandon(string handlerType, string id, double elapsedSeconds)
        {
            var status = GetStatus(handlerType);
            status.AverageAbandonSeconds = (status.AverageAbandonSeconds * status.TotalBusAbandoned + elapsedSeconds)/(status.TotalBusAbandoned+1);            
            Interlocked.Increment(ref status.TotalBusAbandoned);

            var now = _dateTime.UtcNow;
            if (status.AbandonSlowestSeconds < elapsedSeconds || (now-status.AbandonSlowestTime).TotalMinutes > 30)
            {
                status.AbandonSlowestSeconds = elapsedSeconds;
                status.AbandonSlowestId = id;
                status.AbandonSlowestTime = now;
            }

            if (elapsedSeconds > 5)
                Interlocked.Increment(ref status.TotalSlowAbandon);
        }

        public void Abandon(string handlerType, string id, Exception ex)
        {            
            var status = GetStatus(handlerType);

            var now = _dateTime.UtcNow;           

            status.LastTimeout = new HandlerStatus.ExceptionCase
            {
                Date = now,
                Message = ex.Message,
                Stack = ex.StackTrace
            };

            var seconds = (int)(now - now.Date).TotalSeconds;
            status.TimeoutsPerSecond.AddOrUpdate(seconds, i => 1, (i, value) => value+1); 
            status.LastTimeout = new HandlerStatus.ExceptionCase
            {
                Message = ex.Message,
                Stack = ex.StackTrace,
                Date = now
            };

            Interlocked.Increment(ref status.TotalAbandoned);

            Cleanup(status);
        }

        public void Error(string handlerType, string id, Exception ex)
        {
            var status = GetStatus(handlerType);

            var now = _dateTime.UtcNow;
            
            var seconds = (int)(now - now.Date).TotalSeconds;
            status.ExceptionsPerSecond.AddOrUpdate(seconds, i => 1, (i, value) => value+1);
            status.LastException = new HandlerStatus.ExceptionCase
            {
                Message = ex.Message,
                Stack = ex.StackTrace,
                Date = now
            };

            Interlocked.Increment(ref status.TotalErrors);

            Cleanup(status);
        }

        public void Clear()
        {
            _statuses.Statuses.Clear();
        }

        public string ToCsv()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Type,FirstDate,Total,Completed,Abandoned,Errors,LastUpdate,AvgDuration,AvgQueued,MessagePerSec,ExceptionPerSec,TimeoutPerSec,LastException,LastTimeout,SlowestId,SlowestSeconds,SlowestDate,SlowestCompleteId,SlowestCompleteSeconds,SlowestCompleteDate,SlowestCompleteTotal,AverageComplete,SlowestAbandonId,SlowestAbandonSeconds,SlowestAbandonDate,SlowestAbandonTotal,AverageAbandon");

            var keys = _statuses.Statuses.Keys;            

            foreach (var key in keys)
            {
                var status = _statuses.Statuses[key];                
                Cleanup(status);                
                sb.AppendLine($"{key},{status.CreateDate:O},{status.TotalMessages},{status.TotalCompleted},{status.TotalAbandoned},{status.TotalErrors},{status.LastUpdated:O},{status.AverageDurationSeconds},{status.AverageQueuedSeconds},{GetPerSecond(status.MessagesPerSecond)},{GetPerSecond(status.ExceptionsPerSecond)},{GetPerSecond(status.TimeoutsPerSecond)},{GetExceptionCaseMessage(status.LastException)},{GetExceptionCaseMessage(status.LastTimeout)},{status.SlowestId},{status.SlowestSeconds},{status.SlowestTime:O},{status.CompleteSlowestId},{status.CompleteSlowestSeconds},{status.CompleteSlowestTime:O},{status.TotalSlowComplete},{status.AverageCompleteSeconds},{status.AbandonSlowestId},{status.AbandonSlowestSeconds},{status.AbandonSlowestTime:O},{status.TotalSlowAbandon},{status.AverageAbandonSeconds}");
            }

            return sb.ToString();
        }

        private string GetExceptionCaseMessage(HandlerStatus.ExceptionCase statusLastException)
        {
            if (statusLastException == null) return "";

            return $"{statusLastException.Date:O} - {statusLastException.Message?.Replace(",",".").Replace("\r","").Replace("\n","--")} {statusLastException.Stack?.Replace(",",".").Replace("\r","").Replace("\n","--")}";
        }

        private double GetPerSecond(ConcurrentDictionary<int, int> entries)
        {
            var mpsKeys = entries.Keys.OrderBy(x => x);
            var mpsFirst = mpsKeys.FirstOrDefault();
            var mpsLast = mpsKeys.LastOrDefault();
            var mpsSeconds = mpsLast - mpsFirst;
            if (mpsSeconds == 0) return 0;
            return entries.Sum(x => x.Value) / (double)mpsSeconds;
        }
    }
}