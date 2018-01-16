﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.AzureStorage.Tables.Paging;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
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

        public async Task<bool> CreateIfNotExistsAsync(AddressMonitorType monitorType, string address)
        {
            var partitionKey = GetPartitionKey(monitorType);
            var rowKey = GetRowKey(address);

            return await _tableStorage.CreateIfNotExistsAsync(new AddressEntity(partitionKey, rowKey));
        }

        public async Task<bool> DeleteIfExistAsync(AddressMonitorType monitorType, string address)
        {
            var partitionKKey = GetPartitionKey(monitorType);
            var rowKey = GetRowKey(address);

            return await _tableStorage.DeleteIfExistAsync(partitionKKey, rowKey);
        }

        public async Task<IAddress> GetAsync(AddressMonitorType monitorType, string address)
        {
            var partitionKKey = GetPartitionKey(monitorType);
            var rowKey = GetRowKey(address);

            return await _tableStorage.GetDataAsync(partitionKKey, rowKey);
        }

        public async Task<(string continuation, IEnumerable<IAddress> items)> GetByTypeAsync(AddressMonitorType monitorType, string continuation = null, int take = 100)
        {
            var pagingInfo = new PagingInfo { ElementCount = take };
            
            pagingInfo.Decode(continuation);

            var query = new TableQuery<AddressEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, GetPartitionKey(monitorType)));

            var items = await _tableStorage.ExecuteQueryWithPaginationAsync(query, pagingInfo);

            return (pagingInfo.Encode(), items);
        }
    }
}
