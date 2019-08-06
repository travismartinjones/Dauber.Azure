using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HighIronRanch.Azure.ServiceBus.Contracts;
using HighIronRanch.Azure.TableStorage;
using Microsoft.WindowsAzure.Storage.Table;

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
            var table = _tableService.GetTable(EVENT_STORE_TABLE_NAME, false);
            var entity = new ScheduledMessage(sessionId, messageId, sequenceId, type, submitDate, scheduleEnqueueDate);
            var insertOperation = TableOperation.Insert(entity);
            await table.ExecuteAsync(insertOperation).ConfigureAwait(false);
        }

        public async Task Cancel(string sessionId, string messageId)
        {
            var table = _tableService.GetTable(EVENT_STORE_TABLE_NAME, false);            

            var query = new TableQuery<ScheduledMessage>()
                .Where(TableQuery
                    .CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, messageId)
                    )
                );

            var entity = table.ExecuteQuery(query).FirstOrDefault();
            if (entity == null) return;            
            entity.IsCancelled = true;
            var insertOperation = TableOperation.InsertOrReplace(entity);
            await table.ExecuteAsync(insertOperation).ConfigureAwait(false);
        }

        public async Task Delete(string sessionId, string messageId)
        {
            var table = _tableService.GetTable(EVENT_STORE_TABLE_NAME, false);
            
            var query = new TableQuery<ScheduledMessage>()
                .Where(TableQuery
                    .CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, messageId)
                    ));

            var sagaStateRow = table.ExecuteQuery(query).FirstOrDefault();
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
            var table = _tableService.GetTable(EVENT_STORE_TABLE_NAME, false);            

            var query = new TableQuery<ScheduledMessage>()
                .Where(TableQuery
                    .CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, messageId)
                    )
                );

            return ConvertToDomainEvent(table.ExecuteQuery(query)).FirstOrDefault();       
        }

        public async Task<List<HighIronRanch.Azure.ServiceBus.Contracts.ScheduledMessage>> GetBySessionIdType(string sessionId, string type)
        {
            var table = _tableService.GetTable(EVENT_STORE_TABLE_NAME, false);            

            var query = new TableQuery<ScheduledMessage>()
                .Where(TableQuery
                    .CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, type)
                    )
                );

            return ConvertToDomainEvent(table.ExecuteQuery(query));            
        }

        public async Task<List<HighIronRanch.Azure.ServiceBus.Contracts.ScheduledMessage>> GetBySessionIdTypeScheduledDateRange(string sessionId, string type, DateTime startDate, DateTime endDate)
        {
            var table = _tableService.GetTable(EVENT_STORE_TABLE_NAME, false);

            var keyFilter = TableQuery
                .CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, type));
            
            var query = new TableQuery<ScheduledMessage>()
                .Where(keyFilter);

            var all = table.ExecuteQuery(query).ToList();

            // now that we have every message for a type, take this opportunity to clear out any old messages
            await CleanupExpiredMessages(all);

            var results = all.Where(x => x.ScheduleEnqueueDate >= startDate && x.ScheduleEnqueueDate <= endDate).ToList();

            return ConvertToDomainEvent(results); 
        }

        private async Task CleanupExpiredMessages(List<ScheduledMessage> all)
        {
            foreach (var expired in all.Where(x => x.ScheduleEnqueueDate < DateTime.UtcNow))
                await Delete(expired.PartitionKey, expired.RowKey);
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
