using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HighIronRanch.Azure.ServiceBus.Contracts;
using HighIronRanch.Azure.TableStorage;
using Microsoft.Azure.Cosmos.Table;

namespace Dauber.Cqrs.Azure.ServiceBus
{
    public class ScheduledMessageRepository : IScheduledMessageRepository
    {
        private readonly IAzureTableService _tableService;
        public const string EVENT_STORE_TABLE_NAME = "ScheduledMessages";        

        public ScheduledMessageRepository(
            IAzureTableService tableService)
        {
            _tableService = tableService;
        }

        public class ScheduledMessage : BsonPayloadTableEntity
        {
            public long SequenceId { get; set; }
            public string Type { get; set; }
            public DateTime SubmitDate { get; set; }
            public DateTime ScheduleEnqueueDate { get; set; }
            public bool IsCancelled { get; set; }
            protected override int AdditionalPropertySizes => 0;

            public ScheduledMessage() { }

            public ScheduledMessage(string sessionId, string messageId, long sequenceId, string type, DateTime submitDate, DateTime scheduleEnqueueDate)
            {
                PartitionKey = sessionId;
                RowKey = messageId;
                SequenceId = sequenceId;
                Type = type;
                SubmitDate = submitDate;
                ScheduleEnqueueDate = scheduleEnqueueDate;
            }
        }
        
        public async Task Insert(string sessionId, string messageId, long sequenceId, string type, DateTime submitDate, DateTime scheduleEnqueueDate)
        {
            var table = await _tableService.GetTable(EVENT_STORE_TABLE_NAME, false).ConfigureAwait(false);
            var entity = new ScheduledMessage(sessionId, messageId, sequenceId, type, submitDate, scheduleEnqueueDate);
            var insertOperation = TableOperation.Insert(entity);
            await table.ExecuteAsync(insertOperation).ConfigureAwait(false);
        }

        public async Task Cancel(string sessionId, string messageId)
        {
            var table = await _tableService.GetTable(EVENT_STORE_TABLE_NAME, false).ConfigureAwait(false);            

            var query = new TableQuery<ScheduledMessage>()
                .Where(TableQuery
                    .CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, messageId)
                    )
                );

            
            var results = new List<ScheduledMessage>();
            TableContinuationToken continuationToken = null;
            do
            {
                var result = await table.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                if (result.Results?.Count > 0)
                {
                    results.AddRange(result.Results);
                }
                continuationToken = result.ContinuationToken;
            } while (continuationToken != null);

            var entity = results.FirstOrDefault();

            if (entity == null) return;            
            entity.IsCancelled = true;
            var insertOperation = TableOperation.InsertOrReplace(entity);
            await table.ExecuteAsync(insertOperation).ConfigureAwait(false);
        }

        public async Task Delete(string sessionId, string messageId)
        {
            var table = await _tableService.GetTable(EVENT_STORE_TABLE_NAME, false).ConfigureAwait(false);
            
            var query = new TableQuery<ScheduledMessage>()
                .Where(TableQuery
                    .CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, messageId)
                    ));

            
            var results = new List<ScheduledMessage>();
            TableContinuationToken continuationToken = null;
            do
            {
                var result = await table.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                if (result.Results?.Count > 0)
                {
                    results.AddRange(result.Results);
                }
                continuationToken = result.ContinuationToken;
            } while (continuationToken != null);

            var sagaStateRow = results.FirstOrDefault();
            if (sagaStateRow == null) return;                        
            var deleteOperation = TableOperation.Delete(sagaStateRow);
            try
            {
                await table.ExecuteAsync(deleteOperation).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (e.InnerException is WebException webException && 
                    webException.Response!=null && 
                    webException.Response is HttpWebResponse response && 
                    (int)response.StatusCode!=404)
                {
                    // ignore delete operations if the row doesn't exist
                    throw;
                }
            }
        }

        public async Task<HighIronRanch.Azure.ServiceBus.Contracts.ScheduledMessage> GetBySessionIdMessageId(string sessionId, string messageId)
        {
            var table = await _tableService.GetTable(EVENT_STORE_TABLE_NAME, false).ConfigureAwait(false);            

            var query = new TableQuery<ScheduledMessage>()
                .Where(TableQuery
                    .CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, messageId)
                    )
                );            

            var results = new List<ScheduledMessage>();
            TableContinuationToken continuationToken = null;
            do
            {
                var result = await table.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                if (result.Results?.Count > 0)
                {
                    results.AddRange(result.Results);
                }
                continuationToken = result.ContinuationToken;
            } while (continuationToken != null);

            return ConvertToDomainEvent(results).FirstOrDefault();       
        }

        public async Task<List<HighIronRanch.Azure.ServiceBus.Contracts.ScheduledMessage>> GetBySessionIdType(string sessionId, string type)
        {
            var table = await _tableService.GetTable(EVENT_STORE_TABLE_NAME, false).ConfigureAwait(false);            

            var query = new TableQuery<ScheduledMessage>()
                .Where(TableQuery
                    .CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, type)
                    )
                );

            var results = new List<ScheduledMessage>();
            TableContinuationToken continuationToken = null;
            do
            {
                var result = await table.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                if (result.Results?.Count > 0)
                {
                    results.AddRange(result.Results);
                }
                continuationToken = result.ContinuationToken;
            } while (continuationToken != null);

            return ConvertToDomainEvent(results);            
        }

        public async Task<List<HighIronRanch.Azure.ServiceBus.Contracts.ScheduledMessage>> GetBySessionIdTypeScheduledDateRange(string sessionId, string type, DateTime startDate, DateTime endDate)
        {
            var table = await _tableService.GetTable(EVENT_STORE_TABLE_NAME, false).ConfigureAwait(false);

            var keyFilter = TableQuery
                .CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, type));
            
            var query = new TableQuery<ScheduledMessage>()
                .Where(keyFilter);

            
            var all = new List<ScheduledMessage>();
            TableContinuationToken continuationToken = null;
            do
            {
                var result = await table.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                if (result.Results?.Count > 0)
                {
                    all.AddRange(result.Results);
                }
                continuationToken = result.ContinuationToken;
            } while (continuationToken != null);
            
            // now that we have every message for a type, take this opportunity to clear out any old messages
            await CleanupExpiredMessages(all).ConfigureAwait(false);

            var results = all.Where(x => x.ScheduleEnqueueDate >= startDate && x.ScheduleEnqueueDate <= endDate).ToList();

            return ConvertToDomainEvent(results); 
        }

        private async Task CleanupExpiredMessages(List<ScheduledMessage> all)
        {
            foreach (var expired in all.Where(x => x.ScheduleEnqueueDate < DateTime.UtcNow))
                await Delete(expired.PartitionKey, expired.RowKey).ConfigureAwait(false);
        }

        private List<HighIronRanch.Azure.ServiceBus.Contracts.ScheduledMessage> ConvertToDomainEvent(IEnumerable<ScheduledMessage> query)
        {
            return query.Select(x => new HighIronRanch.Azure.ServiceBus.Contracts.ScheduledMessage
            {
                SessionId = x.PartitionKey,                
                CorrelationId = x.RowKey,
                SequenceId = x.SequenceId,
                Type = x.Type,
                SubmitDate = x.SubmitDate,
                ScheduleEnqueueDate = x.ScheduleEnqueueDate
            }).ToList();
        }
    }
}
