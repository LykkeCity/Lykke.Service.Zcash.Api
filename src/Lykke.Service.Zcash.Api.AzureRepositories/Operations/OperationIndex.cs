using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Operations
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class OperationIndex : AzureTableEntity, IOperationIndex
    {
        public OperationIndex()
        {
        }

        public OperationIndex(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public Guid OperationId { get; set; }
    }
}
