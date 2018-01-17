using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class RecentTransaction : ITransaction
    {
        public ObservationSubject ObservationSubject { get; set; }
        public Guid OperationId { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public string Hash { get; set; }
    }
}
