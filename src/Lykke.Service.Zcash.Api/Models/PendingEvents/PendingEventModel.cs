using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.Zcash.Api.Core.Domain.Events;

namespace Lykke.Service.Zcash.Api.Models.PendingEvents
{
    public class PendingEventModel
    {
        public PendingEventModel(IPendingEvent pendingEvent)
        {
            OperationId = pendingEvent.OperationId;
            Timestamp = pendingEvent.CreatedUtc;
            FromAddress = pendingEvent.FromAddress;
            AssetId = pendingEvent.AssetId;
            Amount = pendingEvent.Amount;
            ToAddress = pendingEvent.ToAddress;
            TransactionHash = pendingEvent.TransactionHash;
        }

        public Guid OperationId { get; set; }
        public DateTime Timestamp { get; set; }
        public string FromAddress { get; set; }
        public string AssetId { get; set; }
        public string Amount { get; set; }
        public string ToAddress { get; set; }
        public string TransactionHash { get; set; }
    }
}
