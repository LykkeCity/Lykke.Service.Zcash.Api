using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.AzureStorage.Tables;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.History;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Zcash.Api.AzureRepositories.History
{
    public class HistoryRepository : IHistoryRepository
    {
        private INoSQLTableStorage<HistoricalTransactionEntity> _historyStorage;
        private INoSQLTableStorage<IndexEntity> _indexStorage;

        private static string GetPartitionKey(ObservationSubject subject, string address) => $"{Enum.GetName(typeof(ObservationSubject), subject)}_{address}";
        private static string GetRowKey(long timestamp, string hash) => $"{timestamp}_{hash}";

        public HistoryRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _historyStorage = AzureTableStorage<HistoricalTransactionEntity>.Create(connectionStringManager, "ZcashHistory", log);
            _indexStorage = AzureTableStorage<IndexEntity>.Create(connectionStringManager, "ZcashHistoryIndex", log);
        }

        public Task UpsertAsync(ObservationSubject subject, Guid operationId, string hash, DateTime timestampUtc, string fromAddress, string toAddress, decimal amount, string assetId)
        {
            var observableAddress = subject == ObservationSubject.From ? fromAddress : toAddress;
            var timestamp = DateTimeOffset.UtcNow. timestampUtc.to
            var entity = new HistoricalTransactionEntity(GetPartitionKey(subject, observableAddress), GetRowKey())
        }

        public Task<IEnumerable<ITransaction>> GetByAddressAsync(ObservationSubject type, string address, string afterHash = null, int take = 100)
        {
            throw new NotImplementedException();
        }

        public class IndexEntity : TableEntity
        {
            public long TimestampUtc { get; set; }
        }
    }
}
