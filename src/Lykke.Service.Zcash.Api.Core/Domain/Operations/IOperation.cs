using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public interface IOperation
    {
        Guid OperationId { get; }
        OperationState State { get; }
        OperationType Type { get; }
        DateTime TimestampUtc { get; }
        DateTime BuiltUtc { get; }
        DateTime? SentUtc { get; }
        DateTime? CompletedUtc { get; }
        DateTime? FailedUtc { get; }
        DateTime? DeletedUtc { get; }
        string Hash { get; }
        string Error { get; }
        string AssetId { get; }
        decimal Amount { get; }
        decimal Fee { get; }
        bool SubtractFee { get; }

        IOperationItem[] Items { get; }
    }
}
