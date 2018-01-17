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
        private static string GetTxPartitionKey() => "Operation";
        private static string GetTxRowKey(Guid operationId) => operationId.ToString();
        private static string GetIxPartitionKey() => "OperationIndex";
        private static string GetIxRowKey(string hash) => hash;

        public OperationRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _operationStorage = AzureTableStorage<OperationalTransactionEntity>.Create(connectionStringManager, "ZcashOperations", log);
            _indexStorage = AzureTableStorage<IndexEntity>.Create(connectionStringManager, "ZcashOperationIndex", log);
        }

        public async Task<IOperationalTransaction> CreateAsync(Guid operationId, string fromAddress, string toAddress, string assetId, decimal amount, decimal? fee, string signContext)
        {
            var entity = new OperationalTransactionEntity(GetTxPartitionKey(), GetTxRowKey(operationId))
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

        public async Task<IOperationalTransaction> UpdateAsync(Guid operationId, DateTime? sentUtc = null, DateTime? completedUtc = null, DateTime? failedUtc = null,
            string signedTransaction = null, string hash = null, string error = null)
        {
            if (!string.IsNullOrWhiteSpace(hash))
            {
                await _indexStorage.InsertOrReplaceAsync(new IndexEntity
                {
                    PartitionKey = GetIxPartitionKey(),
                    RowKey = GetIxRowKey(hash),
                    OperationId = operationId
                });
            }

            return await _operationStorage.MergeAsync(GetTxPartitionKey(), GetTxRowKey(operationId), e =>
            {
                e.SentUtc = sentUtc ?? e.SentUtc;
                e.CompletedUtc = completedUtc ?? e.CompletedUtc;
                e.FailedUtc = failedUtc ?? e.FailedUtc;
                e.SignedTransaction = signedTransaction ?? e.SignedTransaction;
                e.Hash = hash ?? e.Hash;
                e.Error = error ?? e.Error;
                return e;
            });
        }

        public async Task<bool> DeleteIfExistAsync(Guid operationId)
        {
            return await _operationStorage.DeleteIfExistAsync(GetTxPartitionKey(), GetTxRowKey(operationId));
        }

        public async Task<IOperationalTransaction> GetAsync(Guid operationId)
        {
            return await _operationStorage.GetDataAsync(GetTxPartitionKey(), GetTxRowKey(operationId));
        }

        public async Task<IEnumerable<IOperationalTransaction>> GetByStateAsync(OperationState state)
        {
            return await _operationStorage.GetDataAsync(GetTxPartitionKey(), tx => tx.State == state);
        }

        public async Task<Guid?> GetOperationIdAsync(string hash)
        {
            var partitionKey = GetIxPartitionKey();
            var rowKey = GetIxRowKey(hash);

            return (await _indexStorage.GetDataAsync(partitionKey, rowKey))?.OperationId;
        }

        public class IndexEntity : TableEntity
        {
            public Guid OperationId { get; set; }
        }
    }
}
