using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public interface IOperation
    {
        Guid OperationId { get; }
        OperationState State { get; }
        DateTime BuiltUtc { get; }
        DateTime? SentUtc { get; }
        DateTime? CompletedUtc { get; }
        DateTime? FailedUtc { get; }
        DateTime? DeletedUtc { get; }
        string SignContext { get; }
        string SignedTransaction { get; }
        string Hash { get; }
        string Error { get; }
        decimal Amount { get; }
        decimal Fee { get; }

        IOperationItem[] Items { get; }
    }
}
