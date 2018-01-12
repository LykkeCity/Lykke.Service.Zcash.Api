using System;
using System.Collections.Generic;
using System.Text;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Zcash.Api.Core.Domain.Events;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Transactions
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class TransactionEntity : AzureTableEntity, ITransaction
    {
        public TransactionEntity()
        {
        }

        public TransactionEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public TransactionState State { get; set; }
        public Guid OperationId { get; set; }
        public DateTime BuiltUtc { get; set; }
        public DateTime? SentUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public DateTime? FailedUtc { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string AssetId { get; set; }
        public string Amount { get; set; }
        public string SignContext { get; }
        public string SignedTransaction { get; set; }
        public string Hash { get; set; }
        public string Error { get; set; }
    }
}
