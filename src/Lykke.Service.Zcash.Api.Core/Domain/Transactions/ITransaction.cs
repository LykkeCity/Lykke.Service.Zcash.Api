using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Transactions
{
    public interface ITransaction
    {
        TransactionState State { get; }
        Guid OperationId { get; }
        DateTime BuiltUtc { get; }
        DateTime? SentUtc { get; }
        DateTime? CompletedUtc { get; }
        DateTime? FailedUtc { get; }
        string FromAddress { get; }
        string ToAddress { get; }
        string AssetId { get; }
        decimal Amount { get; }
        decimal Fee { get; set; }
        string SignContext { get; set; }
        string SignedTransaction { get; set; }
        string Hash { get; set; }
        string Error { get; set; }
    }
}
