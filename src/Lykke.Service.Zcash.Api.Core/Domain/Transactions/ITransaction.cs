using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Transactions
{
    public interface ITransaction
    {
        Guid OperationId { get; }
        string FromAddress { get; }
        string ToAddress { get; }
        string AssetId { get; }
        decimal Amount { get; }
        decimal? Fee { get; }
        string Hash { get; }
        DateTime Timestamp { get; }
    }
}
