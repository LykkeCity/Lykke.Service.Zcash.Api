using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.AzureStorage.Tables;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Operations
{
    public class OperationRepository : IOperationRepository
    {
        private INoSQLTableStorage<OperationEntity> _operationStorage;
        private INoSQLTableStorage<OperationItemEntity> _operationItemStorage;
        private INoSQLTableStorage<IndexEntity> _indexStorage;
        private static string GetOperationPartitionKey(Guid operationId) => operationId.ToString();
        private static string GetOperationRowKey() => string.Empty;
        private static string GetOperationItemPartitionKey(Guid operationId) => operationId.ToString();
        private static string GetOperationItemRowKey() => string.Empty;
        private static string GetIndexPartitionKey(string hash) => hash;
        private static string GetIndexRowKey() => string.Empty;

        public OperationRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _operationStorage = AzureTableStorage<OperationEntity>.Create(connectionStringManager, "ZcashOperations", log);
            _operationItemStorage = AzureTableStorage<OperationItemEntity>.Create(connectionStringManager, "ZcashOperationItems", log);
            _indexStorage = AzureTableStorage<IndexEntity>.Create(connectionStringManager, "ZcashOperationIndex", log);
        }

        public async Task<IOperation> UpsertAsync(Guid operationId, OperationType type, (string fromAddress, string toAddress, decimal amount)[] items,
            decimal fee, bool subtractFee, string assetId)
        {
            var operationItemEntities = items
                .Select(item => new OperationItemEntity()
                {
                    PartitionKey = GetOperationItemPartitionKey(operationId),
                    RowKey = GetOperationItemRowKey(),
                    Amount = item.amount,
                    FromAddress = item.fromAddress,
                    ToAddress = item.toAddress
                })
                .ToArray();

            var operationEntity = new OperationEntity()
            {
                PartitionKey = GetOperationPartitionKey(operationId),
                RowKey = GetOperationRowKey(),
                Amount = items.Sum(item => item.amount),
                Fee = fee,
                SubtractFee = subtractFee,
                AssetId = assetId,
                State = OperationState.Built,
                BuiltUtc = DateTime.UtcNow,
                Items = operationItemEntities
            };

            await _operationStorage.InsertOrReplaceAsync(operationEntity);

            await _operationItemStorage.InsertOrReplaceAsync(operationItemEntities);

            return operationEntity;
        }

        public async Task<IOperation> UpdateAsync(Guid operationId,
            DateTime? sentUtc = null, DateTime? completedUtc = null, DateTime? failedUtc = null, DateTime? deletedUtc = null,
            string hash = null, string error = null)
        {
            var partitionKey = GetOperationPartitionKey(operationId);
            var rowKey = GetOperationRowKey();
            var entity = await _operationStorage.MergeAsync(partitionKey, rowKey, e =>
            {
                e.State =
                    deletedUtc.HasValue ? OperationState.Deleted :
                    failedUtc.HasValue ? OperationState.Failed :
                    completedUtc.HasValue ? OperationState.Completed :
                    sentUtc.HasValue ? OperationState.Sent :
                    e.State;
                e.SentUtc = sentUtc ?? e.SentUtc;
                e.CompletedUtc = completedUtc ?? e.CompletedUtc;
                e.FailedUtc = failedUtc ?? e.FailedUtc;
                e.DeletedUtc = deletedUtc ?? e.DeletedUtc;
                e.Hash = hash ?? e.Hash;
                e.Error = error ?? e.Error;
                return e;
            });

            await UpsertIndexAsync(hash, operationId);

            return entity;
        }

        public async Task<IOperation> GetAsync(Guid operationId, bool loadItems = true)
        {
            var partitionKKey = GetOperationPartitionKey(operationId);
            var rowKey = GetOperationRowKey();
            var operation = await _operationStorage.GetDataAsync(partitionKKey, rowKey);

            if (loadItems && operation != null)
            {
                operation.Items = (await _operationItemStorage.GetDataAsync(GetOperationItemPartitionKey(operationId))).ToArray();    
            }

            return operation;
        }

        public async Task<IOperation> GetAsync(string hash, bool loadItems = true)
        {
            var partitionKey = GetIndexPartitionKey(hash);
            var rowKey = GetIndexRowKey();
            var entity = await _indexStorage.GetDataAsync(partitionKey, rowKey);

            if (loadItems && entity != null)
            {
                return await GetAsync(entity.OperationId, loadItems);
            }

            return null;
        }

        public async Task UpsertIndexAsync(string hash, Guid operationId)
        {
            if (!string.IsNullOrWhiteSpace(hash))
            {
                await _indexStorage.InsertOrReplaceAsync(
                    new IndexEntity
                    {
                        PartitionKey = GetIndexPartitionKey(hash),
                        RowKey = GetIndexRowKey(),
                        OperationId = operationId
                    });
            }   
        }

        public class IndexEntity : TableEntity
        {
            public Guid OperationId { get; set; }
        }
    }
}
