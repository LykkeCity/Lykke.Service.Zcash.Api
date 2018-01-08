using System.Numerics;

namespace Lykke.Service.Zcash.Api.Core.Domain.Insight
{
    public class Utxo
    {
        public string Address { get; set; }
        public string TxId { get; set; }
        public uint Vout { get; set; }
        public string ScriptPubKey { get; set; }
        public decimal Amount { get; set; }
        public ulong Satoshis { get; set; } // UInt64 is enough for 2_100_000_000_000_000 of zatoshis
        public ulong? Confirmations { get; set; }
        public ulong? Height { get; set; }
    }
}
