using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Cqrs.Contracts;
using HighIronRanch.Azure.TableStorage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Dauber.Cqrs.Azure.Saga
{
    public class SagaStateRepository : ISagaStateRepository
    {
        private readonly IAzureTableService _tableService;
        public const string EVENT_STORE_TABLE_NAME = "Sagas";        

        public SagaStateRepository(
            IAzureTableService tableService)
        {
            _tableService = tableService;
        }

        public class SagaState : BsonPayloadTableEntity
        {
            protected override int AdditionalPropertySizes => 0;

            public SagaState() { }

            public SagaState(Guid correlationId, object state)
            {
                PartitionKey = correlationId.ToString();
                RowKey = state.GetType().FullName;

                var domainEventData = state.ToBson();

                if (domainEventData.Length > MaxByteCapacity)
                {
                    throw new ArgumentException($"Event size of {domainEventData.Length} when stored as json exceeds Azure property limit of 960K");
                }

                SetData(domainEventData);
            }
        }

        public async Task<Contracts.SagaState> Read(Guid correlationId)
        {
            var table = await _tableService.GetTable(EVENT_STORE_TABLE_NAME, false).ConfigureAwait(false);

            var query = new TableQuery<SagaState>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, correlationId.ToString()));

            var results = new List<SagaState>();
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
            if (sagaStateRow == null) return null;
            var stateType = GetTypeFromFullName(sagaStateRow.RowKey);
            return new Contracts.SagaState
            {
                StateType = stateType,
                State = sagaStateRow.GetData().FromBson(stateType)
            };
        }

        public async Task Create(Guid correlationId, object state)
        {
            var table = await _tableService.GetTable(EVENT_STORE_TABLE_NAME, false).ConfigureAwait(false);
            var entity = new SagaState(correlationId, state);
            var insertOperation = TableOperation.Insert(entity);
            await table.ExecuteAsync(insertOperation).ConfigureAwait(false);
        }

        public async Task Update(Guid correlationId, object state)
        {
            var table = await _tableService.GetTable(EVENT_STORE_TABLE_NAME, false).ConfigureAwait(false);
            var entity = new SagaState(correlationId, state);
            var insertOperation = TableOperation.InsertOrReplace(entity);
            await table.ExecuteAsync(insertOperation).ConfigureAwait(false);
        }

        public async Task Delete(Guid correlationId)
        {
            var table = await _tableService.GetTable(EVENT_STORE_TABLE_NAME, false).ConfigureAwait(false);
            
            var query = new TableQuery<SagaState>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, correlationId.ToString()));

            var results = new List<SagaState>();
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
            var insertOperation = TableOperation.Delete(sagaStateRow);
            await table.ExecuteAsync(insertOperation).ConfigureAwait(false);
        }

        private Type GetTypeFromFullName(string typeName)
        {
            Type type;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var firstDotIndex = typeName.IndexOf('.');

            if (firstDotIndex > 0)
            {
                // the type most likely lives inside an assembly with a similar namespace
                // by looking at these first, we can skip any external assemblies

                var firstNamespacePart = typeName.Substring(0, firstDotIndex);
                foreach (var assembly in assemblies.Where(assembly => assembly.FullName.StartsWith(firstNamespacePart)))
                {
                    type = assembly.GetType(typeName);

                    if (type != null)
                        return type;
                }
            }

            // we were unable to find the type in an assembly with a similar namespace
            // as a fallback, scan every assembly to make a type match
            foreach (var assembly in assemblies)
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}