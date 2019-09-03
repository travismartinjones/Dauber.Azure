using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dauber.Core.Time;
using HighIronRanch.Azure.ServiceBus.Contracts;
using DateTime = System.DateTime;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public interface ICsvHandlerStatusProcessor : IHandlerStatusProcessor
    {
        string ToCsv();
    }

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
            var status = _statuses.Statuses.GetOrAdd(handlerType, s => new HandlerStatus.Status
            {
               AverageDurationSeconds = new HandlerStatus.Average(),
               AverageQueuedSeconds = new HandlerStatus.Average(),
               MessagesPerSecond = new ConcurrentDictionary<int, int>(),
               ExceptionsPerSecond = new ConcurrentDictionary<int, int>(),
               TimeoutsPerSecond = new ConcurrentDictionary<int, int>(),
               IdTracking = new ConcurrentDictionary<string, HandlerStatus.IdTrack>(),
               LastException = new HandlerStatus.ExceptionCase(),
               LastTimeout = new HandlerStatus.ExceptionCase()        
            });

            var now = _dateTime.UtcNow;
            var idTrack = status.IdTracking.GetOrAdd(id, s => new HandlerStatus.IdTrack());
            
            lock(idTrack)
            {
                idTrack.Start = now;
            }

            status.AverageQueuedSeconds.Value = (status.AverageQueuedSeconds.Value * status.AverageQueuedSeconds.Count + (now - queuedDate).TotalSeconds)/(status.AverageQueuedSeconds.Count+1.0);
            status.AverageQueuedSeconds.Count++;
            
            var seconds = (int)(now - now.Date).TotalSeconds;
            status.MessagesPerSecond.AddOrUpdate(seconds, i => 1, (i, value) => value+1);
            status.LastUpdated = now;

            Cleanup(status);            
        }

        private void Cleanup(HandlerStatus.Status status)
        {
            foreach (var existingIdTrack in status.IdTracking.Keys)
            {
                var exiting = status.IdTracking[existingIdTrack];
                if (exiting?.End != null && (_dateTime.UtcNow - exiting.End.Value).TotalSeconds > 3)
                {
                    status.IdTracking.TryRemove(existingIdTrack, out exiting);
                }
            }

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

        public void Complete(string handlerType, string id)
        {
            var status = _statuses.Statuses.GetOrAdd(handlerType, s => new HandlerStatus.Status
            {
                AverageDurationSeconds = new HandlerStatus.Average(),
                AverageQueuedSeconds = new HandlerStatus.Average(),
                MessagesPerSecond = new ConcurrentDictionary<int, int>(),
                ExceptionsPerSecond = new ConcurrentDictionary<int, int>(),
                IdTracking = new ConcurrentDictionary<string, HandlerStatus.IdTrack>(),
                LastException = new HandlerStatus.ExceptionCase(),
                LastTimeout = new HandlerStatus.ExceptionCase(),
                TimeoutsPerSecond = new ConcurrentDictionary<int, int>()
            });

            var idTrack = status.IdTracking.GetOrAdd(id, s => new HandlerStatus.IdTrack());
            var now = _dateTime.UtcNow;
            lock (idTrack)
            {
                idTrack.End = now;
            }            

            status.AverageDurationSeconds.Value = (status.AverageDurationSeconds.Value * status.AverageDurationSeconds.Count + (idTrack.End.Value - idTrack.Start).TotalSeconds)/(status.AverageDurationSeconds.Count+1.0);
            status.AverageDurationSeconds.Count++;
        }

        public void Abandon(string handlerType, string id, Exception ex)
        {
            var status = _statuses.Statuses.GetOrAdd(handlerType, s => new HandlerStatus.Status
            {
                AverageDurationSeconds = new HandlerStatus.Average(),
                AverageQueuedSeconds = new HandlerStatus.Average(),
                MessagesPerSecond = new ConcurrentDictionary<int, int>(),
                ExceptionsPerSecond = new ConcurrentDictionary<int, int>(),
                IdTracking = new ConcurrentDictionary<string, HandlerStatus.IdTrack>(),
                LastException = new HandlerStatus.ExceptionCase(),
                LastTimeout = new HandlerStatus.ExceptionCase(),
                TimeoutsPerSecond = new ConcurrentDictionary<int, int>()
            });

            var now = _dateTime.UtcNow;
            var idTrack = status.IdTracking.GetOrAdd(id, s => new HandlerStatus.IdTrack());
            lock (idTrack)
            {
                idTrack.End = now;
            }

            status.LastTimeout = new HandlerStatus.ExceptionCase
            {
                Date = now,
                Message = ex.Message,
                Stack = ex.StackTrace
            };

            var seconds = (int)(now - now.Date).TotalSeconds;
            status.TimeoutsPerSecond.AddOrUpdate(seconds, i => 1, (i, value) => value+1);

            status.AverageDurationSeconds.Value = (status.AverageDurationSeconds.Value * status.AverageDurationSeconds.Count + (idTrack.End.Value - idTrack.Start).TotalSeconds)/(status.AverageDurationSeconds.Count+1.0);
            status.AverageDurationSeconds.Count++;
        }

        public void Error(string handlerType, string id, Exception ex)
        {
            var status = _statuses.Statuses.GetOrAdd(handlerType, s => new HandlerStatus.Status
            {
                AverageDurationSeconds = new HandlerStatus.Average(),
                AverageQueuedSeconds = new HandlerStatus.Average(),
                MessagesPerSecond = new ConcurrentDictionary<int, int>(),
                ExceptionsPerSecond = new ConcurrentDictionary<int, int>(),
                IdTracking = new ConcurrentDictionary<string, HandlerStatus.IdTrack>(),
                LastException = null,
                LastTimeout = null,
                TimeoutsPerSecond = new ConcurrentDictionary<int, int>()
            });

            var now = _dateTime.UtcNow;
            var idTrack = status.IdTracking.GetOrAdd(id, s => new HandlerStatus.IdTrack());
            lock (idTrack)
            {
                idTrack.End = now;
            }

            status.LastException = new HandlerStatus.ExceptionCase
            {
                Date = now,
                Message = ex.Message,
                Stack = ex.StackTrace
            };

            var seconds = (int)(now - now.Date).TotalSeconds;
            status.ExceptionsPerSecond.AddOrUpdate(seconds, i => 1, (i, value) => value+1);

            status.AverageDurationSeconds.Value = (status.AverageDurationSeconds.Value * status.AverageDurationSeconds.Count + (idTrack.End.Value - idTrack.Start).TotalSeconds)/(status.AverageDurationSeconds.Count+1.0);
            status.AverageDurationSeconds.Count++;
        }

        public void Clear()
        {
            _statuses.Statuses.Clear();
        }

        public string ToCsv()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Type,AvgDuration,AvgQueued,MessagePerSec,ExceptionPerSec,TimeoutPerSec,LastException,LastTimeout,ActiveIds");

            var keys = _statuses.Statuses.Keys;

            foreach (var key in keys)
            {
                var status = _statuses.Statuses[key];                
                Cleanup(status);
                sb.AppendLine($"{key},{status.AverageDurationSeconds.Value},{status.AverageQueuedSeconds.Value},{GetPerSecond(status.MessagesPerSecond)},{GetPerSecond(status.ExceptionsPerSecond)},{GetPerSecond(status.TimeoutsPerSecond)},{GetExceptionCaseMessage(status.LastException)},{GetExceptionCaseMessage(status.LastTimeout)},{string.Join(";",status.IdTracking.Where(x => !x.Value.End.HasValue).Select(x => $"{x.Key};{x.Value.RapidCount};{x.Value.Start:O}") )}");
            }

            return sb.ToString();
        }

        private string GetExceptionCaseMessage(HandlerStatus.ExceptionCase statusLastException)
        {
            if (statusLastException == null) return "";

            return $"{statusLastException.Date:O} - {statusLastException.Message} {statusLastException.Stack}";
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