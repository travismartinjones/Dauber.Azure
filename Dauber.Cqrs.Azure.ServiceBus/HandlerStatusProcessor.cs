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

    public class NullCsvHandlerStatusProcessor : IHandlerStatusProcessor
    {
        public void Begin(string handlerType, string id, DateTime eventDate)
        {            
        }

        public void Complete(string handlerType, string id)
        {
        }

        public void Abandon(string handlerType, string id, Exception ex)
        {
        }

        public void Error(string handlerType, string id, Exception ex)
        {
        }

        public void Clear()
        {
        }
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
               LastException = null,
               LastTimeout = null
            });

            var now = _dateTime.UtcNow;
            var idTrack = status.IdTracking.GetOrAdd(id, s => new HandlerStatus.IdTrack());
            
            lock(idTrack)
            {
                idTrack.RapidCount++;
                idTrack.Start = now;
            }

            var seconds = (int) (now - now.Date).TotalSeconds;
            status.MessagesPerSecond.AddOrUpdate(seconds, i => 1, (i, value) => value + 1);

            lock (status)
            {
                status.AverageQueuedSeconds.Value = (status.AverageQueuedSeconds.Value * status.AverageQueuedSeconds.Count + (now - queuedDate).TotalSeconds) / (status.AverageQueuedSeconds.Count + 1);
                status.AverageQueuedSeconds.Count++;                
                status.LastUpdated = now;
                status.TotalMessages++;
            }
        }

        private void Cleanup(HandlerStatus.Status status)
        {
            foreach (var existingIdTrack in status.IdTracking.Keys)
            {
                status.IdTracking.TryGetValue(existingIdTrack, out var existing);
                if (existing?.End != null && (_dateTime.UtcNow - existing.End.Value).TotalSeconds > 3)
                {
                    // remove completed messages after 3 seconds
                    status.IdTracking.TryRemove(existingIdTrack, out existing);
                }
                else if (existing != null && !existing.End.HasValue && (_dateTime.UtcNow - existing.Start).TotalMinutes > 5)
                {
                    // remove seemingly stuck messages after 5 minutes
                    status.IdTracking.TryRemove(existingIdTrack, out existing);
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
                TimeoutsPerSecond = new ConcurrentDictionary<int, int>(),
                IdTracking = new ConcurrentDictionary<string, HandlerStatus.IdTrack>(),
                LastException = null,
                LastTimeout =null
            });

            var idTrack = status.IdTracking.GetOrAdd(id, s => new HandlerStatus.IdTrack());
            var now = _dateTime.UtcNow;
            lock (idTrack)
            {
                if (idTrack.Start == DateTime.MinValue)
                    idTrack.Start = now;
                idTrack.End = now;
            }

            var idTotalSeconds = (idTrack.End.Value - idTrack.Start).TotalSeconds;
            if (status.SlowestSeconds < idTotalSeconds)
            {
                status.SlowestSeconds = idTotalSeconds;
                status.SlowestId = id;
                status.SlowestTime = now;
            }

            lock (status)
            {
                status.AverageDurationSeconds.Value = (status.AverageDurationSeconds.Value * status.AverageDurationSeconds.Count + idTotalSeconds)/(status.AverageDurationSeconds.Count+1);
                status.AverageDurationSeconds.Count++;
            }

            Cleanup(status);
        }

        public void Abandon(string handlerType, string id, Exception ex)
        {
            var status = _statuses.Statuses.GetOrAdd(handlerType, s => new HandlerStatus.Status
            {
                AverageDurationSeconds = new HandlerStatus.Average(),
                AverageQueuedSeconds = new HandlerStatus.Average(),
                MessagesPerSecond = new ConcurrentDictionary<int, int>(),
                ExceptionsPerSecond = new ConcurrentDictionary<int, int>(),                
                TimeoutsPerSecond = new ConcurrentDictionary<int, int>(),
                IdTracking = new ConcurrentDictionary<string, HandlerStatus.IdTrack>(),
                LastException = null,
                LastTimeout = null
            });

            var now = _dateTime.UtcNow;
            var idTrack = status.IdTracking.GetOrAdd(id, s => new HandlerStatus.IdTrack());
            lock (idTrack)
            {
                if (idTrack.Start == DateTime.MinValue)
                    idTrack.Start = now;
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

            
            var idTotalSeconds = (idTrack.End.Value - idTrack.Start).TotalSeconds;
            if (status.SlowestSeconds < idTotalSeconds)
            {
                status.SlowestSeconds = idTotalSeconds;
                status.SlowestId = id;
                status.SlowestTime = now;
            }

            lock (status)
            {
                status.AverageDurationSeconds.Value = (status.AverageDurationSeconds.Value * status.AverageDurationSeconds.Count + idTotalSeconds)/(status.AverageDurationSeconds.Count+1);
                status.AverageDurationSeconds.Count++;
            }

            Cleanup(status);
        }

        public void Error(string handlerType, string id, Exception ex)
        {
            var status = _statuses.Statuses.GetOrAdd(handlerType, s => new HandlerStatus.Status
            {
                AverageDurationSeconds = new HandlerStatus.Average(),
                AverageQueuedSeconds = new HandlerStatus.Average(),
                MessagesPerSecond = new ConcurrentDictionary<int, int>(),
                ExceptionsPerSecond = new ConcurrentDictionary<int, int>(),                
                TimeoutsPerSecond = new ConcurrentDictionary<int, int>(),
                IdTracking = new ConcurrentDictionary<string, HandlerStatus.IdTrack>(),
                LastException = null,
                LastTimeout = null
            });

            var now = _dateTime.UtcNow;
            var idTrack = status.IdTracking.GetOrAdd(id, s => new HandlerStatus.IdTrack());
            lock (idTrack)
            {
                if (idTrack.Start == DateTime.MinValue)
                    idTrack.Start = now;
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
            
            var idTotalSeconds = (idTrack.End.Value - idTrack.Start).TotalSeconds;
            if (status.SlowestSeconds < idTotalSeconds)
            {
                status.SlowestSeconds = idTotalSeconds;
                status.SlowestId = id;
                status.SlowestTime = now;
            }

            lock (status)
            {
                status.AverageDurationSeconds.Value = (status.AverageDurationSeconds.Value * status.AverageDurationSeconds.Count + idTotalSeconds)/(status.AverageDurationSeconds.Count+1);
                status.AverageDurationSeconds.Count++;
            }

            Cleanup(status);
        }

        public void Clear()
        {
            _statuses.Statuses.Clear();
        }

        public string ToCsv()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Type,Count,CountStart,LastUpdate,AvgDuration,AvgQueued,MessagePerSec,ExceptionPerSec,TimeoutPerSec,LastException,LastTimeout,SlowestId,SlowestSeconds,SlowestDate,ActiveIds");            

            var keys = _statuses.Statuses.Keys;

            foreach (var key in keys)
            {
                var status = _statuses.Statuses[key];                
                Cleanup(status);
                sb.AppendLine($"{key},{status.TotalMessages},{status.CreateDate:O},{status.LastUpdated:O},{status.AverageDurationSeconds.Value},{status.AverageQueuedSeconds.Value},{GetPerSecond(status.MessagesPerSecond)},{GetPerSecond(status.ExceptionsPerSecond)},{GetPerSecond(status.TimeoutsPerSecond)},{GetExceptionCaseMessage(status.LastException)},{GetExceptionCaseMessage(status.LastTimeout)},{status.SlowestId},{status.SlowestSeconds},{status.SlowestTime:O},{string.Join(",", status.IdTracking.Where(x => !x.Value.End.HasValue).Select(x => $"{x.Key} ({x.Value.RapidCount}) {x.Value.Start:O}".Replace("\n", "").Replace("\r", "")))}");                
            }

            return sb.ToString();
        }

        private string GetExceptionCaseMessage(HandlerStatus.ExceptionCase statusLastException)
        {
            if (statusLastException == null) return "";

            return $"{statusLastException.Date:O} - {statusLastException.Message.Replace(",",".").Replace("\r","").Replace("\n","--")} {statusLastException.Stack.Replace(",",".").Replace("\r","").Replace("\n","--")}";
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