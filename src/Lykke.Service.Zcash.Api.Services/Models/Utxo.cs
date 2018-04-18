namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class Utxo
    {
        public string TxId { get; set; }
        public uint Vout { get; set; }
        public string Address { get; set; }
        public string ScriptPubKey { get; set; }
        public decimal Amount { get; set; }
        public long Confirmations { get; set; }
    }
}
