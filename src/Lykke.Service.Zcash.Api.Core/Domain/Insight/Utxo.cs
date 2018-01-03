using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Insight
{
    public class Utxo
    {
        public string Address { get; set; }
        public string TxId { get; set; }
        public uint Vout { get; set; }
        public string ScriptPubKey { get; set; }
        public decimal Amount { get; set; }
        public BigInteger Satoshis { get; set; }
        public ulong? Confirmations { get; set; }
        public ulong? Height { get; set; }
    }
}
