using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Lykke.Service.Zcash.Api.Core.Domain.Events;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;
using MoreLinq;

namespace Lykke.Service.Zcash.Api.AzureRepositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private INoSQLTableStorage<TransactionEntity> _tableStorage;
        private static string GetPartitionKey() => "tx";
        private static string GetRowKey(Guid operationId) => operationId.ToString();

        public TransactionRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _tableStorage = AzureTableStorage<TransactionEntity>.Create(connectionStringManager, "ZcashTransactions", log);
        }

        public async Task<IReadOnlyList<ITransaction>> Get(TransactionState state, int? limit = int.MaxValue)
        {
            return (await _tableStorage.GetTopRecordsAsync(GetPartitionKey(), limit.Value)).ToArray();
        }

        public async Task<ITransaction> Get(Guid operationId)
        {
            return (await _tableStorage.GetDataRowKeyOnlyAsync(GetRowKey(operationId))).FirstOrDefault();
        }

        public async Task DeleteAsync(IEnumerable<Guid> operationIds)
        {
            if (operationIds == null ||
                operationIds.Count() == 0)
            {
                return;
            }

            foreach (var batch in operationIds.Batch(100))
            {
                var batchOperation = new TableBatchOperation();

                foreach (var id in batch)
                {
                    batchOperation.Delete(new TransactionEntity(GetPartitionKey(), GetRowKey(id)));
                }
                    
                await _tableStorage.DoBatchAsync(batchOperation);
            }
        }


        public Task<IReadOnlyList<ITransaction>> Get(TransactionState? state = null, int skip = 0, int take = 0)
        {
            throw new NotImplementedException();
        }

        public Task<ITransaction> CreateAsync(Guid operationId, string fromAddress, string toAddress, string assetId, string amount, string signContext = null)
        {
            throw new NotImplementedException();
        }

        public Task Update(ITransaction tx)
        {
            throw new NotImplementedException();
        }
    }
}
