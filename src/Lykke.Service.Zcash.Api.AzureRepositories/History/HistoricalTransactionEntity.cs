using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Zcash.Api.Core.Domain;

namespace Lykke.Service.Zcash.Api.AzureRepositories.History
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class HistoricalTransactionEntity : AzureTableEntity, ITransaction
    {
        public HistoricalTransactionEntity()
        {
        }

        public HistoricalTransactionEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public Guid OperationId { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public decimal? Fee { get; set; }
        public string Hash { get; set; }
    }
}
