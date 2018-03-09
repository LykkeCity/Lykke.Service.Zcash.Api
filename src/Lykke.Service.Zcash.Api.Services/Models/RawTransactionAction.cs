using System;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.History;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class RawTransactionAction
    {
        public HistoryAddressCategory Category { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }

        public string AffectedAddress
        {
            get
            {
                return
                    Category == HistoryAddressCategory.From ? FromAddress :
                    Category == HistoryAddressCategory.To ? ToAddress :
                    throw new InvalidOperationException("Invalid transaction action category");
            }
        }
    }
}
