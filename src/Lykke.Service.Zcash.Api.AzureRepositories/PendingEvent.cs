using System;
using System.Collections.Generic;
using System.Text;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Zcash.Api.Core.Domain.Events;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Zcash.Api.AzureRepositories
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class PendingEvent : AzureTableEntity, IPendingEvent
    {
        public PendingEvent()
        {
        }

        public PendingEvent(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        [IgnoreProperty]
        public EventType EventType
        {
            get { return (EventType)Enum.Parse(typeof(EventType), PartitionKey); }
        }

        [IgnoreProperty]
        public Guid OperationId
        {
            get { return Guid.Parse(RowKey); }
        }

        public string    FromAddress     { get; set; }
        public string    AssetId         { get; set; }
        public string    Amount          { get; set; }
        public string    ToAddress       { get; set; }
        public string    TransactionHash { get; set; }
        public DateTime  CreatedUtc      { get; set; }
        public DateTime? DeletedUtc      { get; set; }
    }
}
