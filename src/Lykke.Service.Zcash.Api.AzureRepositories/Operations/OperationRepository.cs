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
        private INoSQLTableStorage<OperationalTransactionEntity> _operationStorage;
        private INoSQLTableStorage<IndexEntity> _indexStorage;
        private static string GetOperationPartitionKey(Guid operationId) => operationId.ToString();
        private static string GetOperationRowKey() => string.Empty;
        private static string GetIndexPartitionKey(string hash) => hash;
        private static string GetIndexRowKey(string addr) => addr;

        public OperationRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _operationStorage = AzureTableStorage<OperationalTransactionEntity>.Create(connectionStringManager, "ZcashOperations", log);
            _indexStorage = AzureTableStorage<IndexEntity>.Create(connectionStringManager, "ZcashOperationIndex", log);
        }

        public async Task<IOperationalTransaction> CreateAsync(Guid operationId, 
            string fromAddress, string toAddress, string assetId, decimal amount, decimal? fee, string signContext)
        {
            var partitionKey = GetOperationPartitionKey(operationId);
            var rowKey = GetOperationRowKey();
            var entity = new OperationalTransactionEntity(partitionKey, rowKey)
            {
                FromAddress = fromAddress,
                ToAddress = toAddress,
                AssetId = assetId,
                Amount = amount,
                Fee = fee,
                SignContext = signContext,
                State = OperationState.Built,
                BuiltUtc = DateTime.UtcNow
            };

            await _operationStorage.InsertAsync(entity);

            return entity;
        }

        public async Task<IOperationalTransaction> UpdateAsync(Guid operationId,
            DateTime? sentUtc = null, DateTime? completedUtc = null, DateTime? failedUtc = null, DateTime? deletedUtc = null,
            string signedTransaction = null, string hash = null, string error = null)
        {
            var partitionKey = GetOperationPartitionKey(operationId);
            var rowKey = GetOperationRowKey();
            var entity = await _operationStorage.MergeAsync(partitionKey, rowKey, e =>
            {
                e.SentUtc = sentUtc ?? e.SentUtc;
                e.CompletedUtc = completedUtc ?? e.CompletedUtc;
                e.FailedUtc = failedUtc ?? e.FailedUtc;
                e.DeletedUtc = deletedUtc ?? e.DeletedUtc;
                e.SignedTransaction = signedTransaction ?? e.SignedTransaction;
                e.Hash = hash ?? e.Hash;
                e.Error = error ?? e.Error;
                return e;
            });

            await UpsertIndexAsync(hash, entity.ToAddress, operationId);

            return entity;
        }

        public async Task<IOperationalTransaction> GetAsync(Guid operationId)
        {
            var partitionKey = GetOperationPartitionKey(operationId);
            var rowKey = GetOperationRowKey();

            return await _operationStorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task<Guid?> GetOperationIdAsync(string hash, string toAddress)
        {
            var partitionKey = GetIndexPartitionKey(hash);
            var rowKey = GetIndexRowKey(toAddress);

            return (await _indexStorage.GetDataAsync(partitionKey, rowKey))?.OperationId;
        }

        public async Task UpsertIndexAsync(string hash, string toAddress, Guid operationId)
        {
            if (!string.IsNullOrWhiteSpace(hash))
            {
                await _indexStorage.InsertOrReplaceAsync(
                    new IndexEntity
                    {
                        PartitionKey = GetIndexPartitionKey(hash),
                        RowKey = GetIndexRowKey(toAddress),
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
