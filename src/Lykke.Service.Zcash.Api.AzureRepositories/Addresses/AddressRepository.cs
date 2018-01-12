using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.AzureStorage.Tables.Paging;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Addresses
{
    public class AddressRepository : IAddressRepository
    {
        private INoSQLTableStorage<AddressEntity> _tableStorage;
        private static string GetPartitionKey(AddressMonitorType monitorType) => Enum.GetName(typeof(AddressMonitorType), monitorType);
        private static string GetRowKey(string address) => address;

        public AddressRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _tableStorage = AzureTableStorage<AddressEntity>.Create(connectionStringManager, "ZcashAddresses", log);
        }

        public async Task CreateAsync(AddressMonitorType monitorType, string address)
        {
            await _tableStorage.InsertOrReplaceAsync(new AddressEntity
            {
                PartitionKey = GetPartitionKey(monitorType),
                RowKey = GetRowKey(address)
            });
        }

        public async Task DeleteAsync(AddressMonitorType monitorType, string address)
        {
            var partitionKKey = GetPartitionKey(monitorType);
            var rowKey = GetRowKey(address);

            await _tableStorage.DeleteAsync(partitionKKey, rowKey);
        }

        public async Task<IAddress> GetAsync(AddressMonitorType monitorType, string address)
        {
            var partitionKKey = GetPartitionKey(monitorType);
            var rowKey = GetRowKey(address);

            return await _tableStorage.GetDataAsync(partitionKKey, rowKey);
        }

        public async Task<PagedResult<IAddress>> GetObservableAddresses(AddressMonitorType monitorType, string continuation = null, int take = 100)
        {
            var pagingInfo = new PagingInfo { ElementCount = take };
            
            pagingInfo.Decode(continuation);

            var query = new TableQuery<AddressEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, GetPartitionKey(monitorType)));

            var res = await _tableStorage.ExecuteQueryWithPaginationAsync(query, pagingInfo);

            return new PagedResult<IAddress>()
            {
                Continuation = pagingInfo.Encode(),
                Items = res.ToArray()
            };
        }
    }
}
