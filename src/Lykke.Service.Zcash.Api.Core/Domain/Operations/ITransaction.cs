using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public interface ITransaction
    {
        Guid OperationId { get; }
        DateTime TimestampUtc { get; }
        string FromAddress { get; }
        string ToAddress { get; }
        string AssetId { get; }
        decimal Amount { get; }
        string Hash { get; }
    }
}
