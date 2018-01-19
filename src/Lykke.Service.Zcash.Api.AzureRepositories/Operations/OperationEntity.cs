using System;
using System.Collections.Generic;
using System.Text;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Operations
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class OperationEntity : AzureTableEntity, IOperation
    {
        public OperationEntity()
        {
        }

        public OperationEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        [IgnoreProperty]
        public Guid OperationId { get => Guid.Parse(PartitionKey); }

        [IgnoreProperty]
        public IOperationItem[] Items { get; set; }

        public OperationState State { get; set; }
        public DateTime BuiltUtc { get; set; }
        public DateTime? SentUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public DateTime? FailedUtc { get; set; }
        public DateTime? DeletedUtc { get; set; }
        public string SignContext { get; set; }
        public string SignedTransaction { get; set; }
        public string Hash { get; set; }
        public string Error { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }

    }
}
