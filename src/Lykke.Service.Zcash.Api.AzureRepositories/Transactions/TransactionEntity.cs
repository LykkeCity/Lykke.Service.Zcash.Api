﻿using System;
using System.Collections.Generic;
using System.Text;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;

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

        public Guid? OperationId { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public decimal? Fee { get; set; }
        public string Hash { get; set; }
    }
}
