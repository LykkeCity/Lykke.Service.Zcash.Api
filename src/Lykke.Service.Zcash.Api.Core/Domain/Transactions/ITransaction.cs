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
        string Context { get; set; }
        string Hash { get; }
        string Hex { get; set; }
        string Error { get; set; }
    }
}
