using System;
using System.Collections.Generic;
using System.Text;
using Lykke.AzureStorage.Tables;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Addresses
{
    public class AddressEntity : AzureTableEntity, IAddress
    {
        [IgnoreProperty]
        public AddressMonitorType MonitorType { get => (AddressMonitorType)Enum.Parse(typeof(AddressMonitorType), PartitionKey); }

        [IgnoreProperty]
        public string Address { get => RowKey; }
    }
}
