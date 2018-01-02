using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Events
{
    public interface IPendingEvent
    {
        EventType EventType       { get; }
        Guid      OperationId     { get; }
        string    FromAddress     { get; }
        string    AssetId         { get; }
        string    Amount          { get; }
        string    ToAddress       { get; }
        string    TransactionHash { get; }
        DateTime  CreatedUtc      { get; }
        DateTime? DeletedUtc      { get; }
    }
}
