using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.SettingsReader;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Addresses
{
    public class AddressRepository : IAddressRepository
    {
        private readonly INoSQLTableStorage<BalanceAddressEntity> _balanceAddressesStorage;
        private readonly INoSQLTableStorage<HistoryAddressEntity> _historyAddressesStorage;

        private static string GetBalancePartitionKey(string address) => address;
        private static string GetBalanceRowKey() => string.Empty;
        private static string GetHistoryPartitionKey(string address) => address;
        private static string GetHistoryRowKey(HistoryAddressCategory category) => Enum.GetName(typeof(HistoryAddressCategory), category);

        public AddressRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _balanceAddressesStorage = AzureTableStorage<BalanceAddressEntity>.Create(connectionStringManager, "ZcashBalanceAddresses", log);
            _historyAddressesStorage = AzureTableStorage<HistoryAddressEntity>.Create(connectionStringManager, "ZcashHistoryAddresses", log);
        }

        public async Task<bool> CreateBalanceAddressIfNotExistsAsync(string address)
        {
            return await _balanceAddressesStorage.CreateIfNotExistsAsync(new BalanceAddressEntity()
            {
                PartitionKey = GetBalancePartitionKey(address),
                RowKey = GetBalanceRowKey()
            });
        }

        public async Task<bool> DeleteBalanceAddressIfExistsAsync(string address)
        {
            var partitionKey = GetBalancePartitionKey(address);
            var rowKey = GetBalanceRowKey();

            return await _balanceAddressesStorage.DeleteIfExistAsync(partitionKey, rowKey);
        }

        public async Task<bool> CreateHistoryAddressIfNotExistsAsync(string address, HistoryAddressCategory category)
        {
            return await _historyAddressesStorage.CreateIfNotExistsAsync(new HistoryAddressEntity()
            {
                PartitionKey = GetHistoryPartitionKey(address),
                RowKey = GetHistoryRowKey(category)
            });
        }

        public async Task<bool> DeleteHistoryAddressIfExistsAsync(string address, HistoryAddressCategory category)
        {
            var partitionKey = GetHistoryPartitionKey(address);
            var rowKey = GetHistoryRowKey(category);

            return await _historyAddressesStorage.DeleteIfExistAsync(partitionKey, rowKey);
        }

        public async Task<(IEnumerable<string> items, string continuation)> GetBalanceAddressesChunkAsync(string continuation = null, int take = 100)
        {
            var chunk = await _balanceAddressesStorage.GetDataWithContinuationTokenAsync(take, continuation);

            return (
                chunk.Entities.Select(e => e.PartitionKey).ToArray(), 
                chunk.ContinuationToken
            );
        }

        public async Task<bool> IsBalanceAddressExistsAsync(string address)
        {
            return await _balanceAddressesStorage.RecordExistsAsync(new BalanceAddressEntity()
            {
                PartitionKey = GetBalancePartitionKey(address),
                RowKey = GetBalanceRowKey()
            });
        }

        public async Task<bool> IsHistoryAddressExistsAsync(string address, HistoryAddressCategory category)
        {
            return await _historyAddressesStorage.RecordExistsAsync(new HistoryAddressEntity()
            {
                PartitionKey = GetHistoryPartitionKey(address),
                RowKey = GetHistoryRowKey(category)
            });
        }
    }
}
