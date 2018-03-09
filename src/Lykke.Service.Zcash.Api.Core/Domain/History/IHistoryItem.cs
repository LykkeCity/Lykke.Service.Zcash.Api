using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.History
{
    public interface IHistoryItem
    {
        Guid? OperationId { get; }
        DateTime TimestampUtc { get; }
        string Hash { get; }
        string FromAddress { get; }
        string ToAddress { get; }
        string AssetId { get; }
        decimal Amount { get; }
    }
}
