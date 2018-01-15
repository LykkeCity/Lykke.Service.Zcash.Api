using System;
using System.Collections.Generic;
using System.Text;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Transactions
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class OperationalTransactionEntity : TransactionEntity, IOperationalTransaction
    {
        public OperationalTransactionEntity() : base()
        {
        }

        public OperationalTransactionEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey)
        {
        }

        public TransactionState State { get; set; }
        public DateTime BuiltUtc { get; set; }
        public DateTime? SentUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public DateTime? FailedUtc { get; set; }
        public string SignContext { get; set; }
        public string SignedTransaction { get; set; }
        public string Error { get; set; }
    }
}
