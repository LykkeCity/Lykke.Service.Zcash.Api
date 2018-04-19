using Newtonsoft.Json;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class Utxo
    {
        [JsonProperty("txid")]
        public string TxId { get; set; }

        [JsonProperty("vout")]
        public uint Vout { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("scriptPubKey")]
        public string ScriptPubKey { get; set; }

        [JsonProperty("redeemScript")]
        public string RedeemScript { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("confirmations")]
        public long Confirmations { get; set; }
    }
}
