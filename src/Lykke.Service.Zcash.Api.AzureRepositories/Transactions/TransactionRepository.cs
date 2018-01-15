using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain.Events;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;
using Lykke.Service.Zcash.Api.Core.Repositories;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;
//using MoreLinq;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Transactions
{
    public class TransactionRepository : IOperationRepository
    {
        private INoSQLTableStorage<TransactionEntity> _tableStorage;
        private static string GetPartitionKey() => "Operation";
        private static string GetRowKey(Guid operationId) => operationId.ToString();

        public TransactionRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _tableStorage = AzureTableStorage<TransactionEntity>.Create(connectionStringManager, "ZcashTransactions", log);

            _tableStorage.ExecuteQueryWithPaginationAsync(
                new TableQuery<TransactionEntity>() {  },
                new AzureStorage.Tables.Paging.PagingInfo() {  })
        }

 

        public async Task<bool> DeleteIfExistAsync(Guid operationId)
        {
            return await _tableStorage.DeleteIfExistAsync(GetPartitionKey(), GetRowKey(operationId));
        }

        public Task<ITransaction> CreateAsync(Guid operationId, string fromAddress, string toAddress, string assetId, string amount, string signContext = null)
        {
            throw new NotImplementedException();
        }

        public Task Update(ITransaction tx)
        {
            throw new NotImplementedException();
        }

        public Task<ITransaction> CreateAsync(Guid operationId, string fromAddress, string toAddress, string assetId, decimal amount, decimal fee, string signContext)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Guid operationId, DateTime? sentUtc = null, DateTime? completedUtc = null, DateTime? failedUtc = null, string signedTransaction = null, string hash = null, string error = null)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<ITransaction>> GetAsync(TransactionState? state = null, string continuation = null, int take = 0)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<ITransaction>> GetAsync(string address, string afterHash = null, int take = 0)
        {
            throw new NotImplementedException();
        }

        public async Task<ITransaction> GetAsync(Guid operationId)
        {
            return (await _tableStorage.GetDataRowKeyOnlyAsync(GetRowKey(operationId))).FirstOrDefault();
        }
    }
}
