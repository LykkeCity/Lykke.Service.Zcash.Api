using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;
using Lykke.SettingsReader;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Operations
{
    public class OperationRepository : IOperationRepository
    {
        private INoSQLTableStorage<OperationalTransactionEntity> _operationStorage;
        private INoSQLTableStorage<OperationIndex> _indexStorage;

        private static string GetPartitionKey() => "Operation";
        private static string GetRowKey(Guid operationId) => operationId.ToString();

        public OperationRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _operationStorage = AzureTableStorage<OperationalTransactionEntity>.Create(connectionStringManager, "ZcashOperations", log);
            _indexStorage = AzureTableStorage<OperationIndex>.Create(connectionStringManager, "ZcashOperationIndex", log);
        }

        public async Task<IOperationalTransaction> CreateAsync(Guid operationId, string fromAddress, string toAddress, string assetId, decimal amount, decimal? fee, string signContext)
        {
            var entity = new OperationalTransactionEntity(GetPartitionKey(), GetRowKey(operationId))
            {
                FromAddress = fromAddress,
                ToAddress = toAddress,
                AssetId = assetId,
                Amount = amount,
                Fee = fee,
                SignContext = signContext,
                State = TransactionState.Built,
                BuiltUtc = DateTime.UtcNow
            };

            await _operationStorage.InsertAsync(entity);

            return entity;
        }

        public async Task<IOperationalTransaction> UpdateAsync(Guid operationId, DateTime? sentUtc = null, DateTime? completedUtc = null, DateTime? failedUtc = null,
            string signedTransaction = null, string hash = null, string error = null)
        {
            return await _operationStorage.MergeAsync(GetPartitionKey(), GetRowKey(operationId), e =>
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
            return await _operationStorage.DeleteIfExistAsync(GetPartitionKey(), GetRowKey(operationId));
        }

        public async Task<IOperationalTransaction> GetAsync(Guid operationId)
        {
            return await _operationStorage.GetDataAsync(GetPartitionKey(), GetRowKey(operationId));
        }

        public async Task<IEnumerable<IOperationalTransaction>> GetByStateAsync(TransactionState state)
        {
            return await _operationStorage.GetDataAsync(GetPartitionKey(), e => e.State == state);
        }

        public Task<IOperationIndex> GetOperationIndexAsync(string transactionHash)
        {
            throw new NotImplementedException();
        }
    }
}
