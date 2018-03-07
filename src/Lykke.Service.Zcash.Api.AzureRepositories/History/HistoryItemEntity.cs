using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Zcash.Api.Core.Domain.History;

namespace Lykke.Service.Zcash.Api.AzureRepositories.History
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class HistoryItemEntity : AzureTableEntity, IHistoryItem
    {
        public HistoryItemEntity()
        {
        }

        public Guid? OperationId { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string Hash { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
    }
}
