using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class TransactionSummary
    {
        public Transaction[] Transactions { get; set; }
        public string LastBlock { get; set; }

        public class Transaction
        {
            public string Address { get; set; }
            public decimal Amount { get; set; }
            public decimal? Fee { get; set; }
            public uint Vout { get; set; }
            public string TxId { get; set; }
            public long BlockTime { get; set; }
            public long Confirmations { get; set; } // can be -1 for unconfirmed
            public string Category { get; set; }
        }
    }
}
