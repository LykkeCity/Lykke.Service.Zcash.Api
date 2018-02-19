using System;
using System.Collections.Generic;
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
        private INoSQLTableStorage<AddressEntity> _tableStorage;
        private static string GetPartitionKey(ObservationCategory category) => Enum.GetName(typeof(ObservationCategory), category);
        private static string GetRowKey(string address) => address;

        public AddressRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _tableStorage = AzureTableStorage<AddressEntity>.Create(connectionStringManager, "ZcashObservableAddresses", log);
        }

        public async Task CreateAsync(ObservationCategory category, string address)
        {
            var partitionKey = GetPartitionKey(category);
            var rowKey = GetRowKey(address);

            await _tableStorage.InsertAsync(new AddressEntity(partitionKey, rowKey));
        }

        public async Task DeleteAsync(ObservationCategory category, string address)
        {
            var partitionKKey = GetPartitionKey(category);
            var rowKey = GetRowKey(address);

            await _tableStorage.DeleteAsync(partitionKKey, rowKey);
        }

        public async Task<IAddress> GetAsync(ObservationCategory category, string address)
        {
            var partitionKKey = GetPartitionKey(category);
            var rowKey = GetRowKey(address);

            return await _tableStorage.GetDataAsync(partitionKKey, rowKey);
        }

        public async Task<(IEnumerable<IAddress> items, string continuation)> GetByCategoryAsync(ObservationCategory category, string continuation = null, int take = 100)
        {
            return await _tableStorage.GetDataWithContinuationTokenAsync(GetPartitionKey(category), take, continuation);
        }

        public async Task<IEnumerable<IAddress>> GetAllAsync()
        {
            return await _tableStorage.GetDataAsync();
        }
    }
}
