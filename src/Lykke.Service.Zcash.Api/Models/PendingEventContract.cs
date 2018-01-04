using Lykke.Service.BlockchainApi.Contract.Responses.PendingEvents;

namespace Lykke.Service.Zcash.Api.Models
{
    public class PendingEventContract : BasePendingEventContract
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string TransactionHash { get; set; }
    }
}
