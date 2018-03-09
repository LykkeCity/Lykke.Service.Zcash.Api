using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Paging;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.History;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Zcash.Api.AzureRepositories.History
{
    public class HistoryRepository : IHistoryRepository
    {
        private INoSQLTableStorage<HistoryItemEntity> _historyStorage;
        private INoSQLTableStorage<IndexEntity> _indexStorage;
        private static string GetHistoryPartitionKey(HistoryAddressCategory category, string address) => $"{Enum.GetName(typeof(HistoryAddressCategory), category)}_{address}";
        private static string GetHistoryRowKey(DateTime timestamp, string hash) => $"{timestamp:yyyyMMddHHmmss}_{hash}";
        private static string GetIndexPartitionKey(string hash) => hash;
        private static string GetIndexRowKey() => string.Empty;

        public HistoryRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _historyStorage = AzureTableStorage<HistoryItemEntity>.Create(connectionStringManager, "ZcashHistory", log);
            _indexStorage = AzureTableStorage<IndexEntity>.Create(connectionStringManager, "ZcashHistoryIndex", log);
        }

        public async Task UpsertAsync(HistoryAddressCategory category, string affectedAddress, DateTime timestampUtc, string hash,
            Guid? operationId, string fromAddress, string toAddress, decimal amount, string assetId)
        {
            await _historyStorage.InsertOrReplaceAsync(new HistoryItemEntity
            {
                PartitionKey = GetHistoryPartitionKey(category, affectedAddress),
                RowKey = GetHistoryRowKey(timestampUtc, hash),
                TimestampUtc = timestampUtc,
                Hash = hash,
                OperationId = operationId,
                FromAddress = fromAddress,
                ToAddress = toAddress,
                Amount = amount,
                AssetId = assetId
            });

            await _indexStorage.InsertOrReplaceAsync(new IndexEntity
            {
                PartitionKey = GetIndexPartitionKey(hash),
                RowKey = GetIndexRowKey(),
                TimestampUtc = timestampUtc
            });
        }

        public async Task<IEnumerable<IHistoryItem>> GetByAddressAsync(HistoryAddressCategory category, string address, string afterHash = null, int take = 100)
        {
            var partitionKey = GetHistoryPartitionKey(category, address);

            if (!string.IsNullOrWhiteSpace(afterHash))
            { 
                var index = await _indexStorage.GetDataAsync(GetIndexPartitionKey(afterHash), GetIndexRowKey());
                if (index != null)
                {
                    var rowKey = GetHistoryRowKey(index.TimestampUtc, afterHash);
                    var page = new PagingInfo { ElementCount = take };
                    var query = new TableQuery<HistoryItemEntity>()
                        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey))
                        .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, rowKey));

                    return await _historyStorage.ExecuteQueryWithPaginationAsync(query, page);
                }
            }

            return await _historyStorage.GetTopRecordsAsync(partitionKey, take);
        }

        public class IndexEntity : TableEntity
        {
            public DateTime TimestampUtc { get; set; }
        }
    }
}
