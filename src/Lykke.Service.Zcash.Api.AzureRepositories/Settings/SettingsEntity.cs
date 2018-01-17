using System;
using System.Collections.Generic;
using System.Text;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Zcash.Api.Core.Domain.Settings;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Settings
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class SettingsEntity : AzureTableEntity, ISettings
    {
        public SettingsEntity()
        {
        }

        public SettingsEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public int ConfirmationLevel { get; set; }
        public string LastBlockHash { get; set; }
        public decimal FeePerKb { get; set; }
        public decimal MaxFee { get; set; }
        public decimal MinFee { get; set; }
        public bool UseDefaultFee { get; set; }
    }
}
