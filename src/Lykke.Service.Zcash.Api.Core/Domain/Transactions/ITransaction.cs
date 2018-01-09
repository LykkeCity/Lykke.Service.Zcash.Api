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
        string Amount { get; }
        string SignContext { get; set; }
        string SignHex { get; set; }
        string Hash { get; set; }
        string Error { get; set; }
        string SigningContext { get; }
    }
}
