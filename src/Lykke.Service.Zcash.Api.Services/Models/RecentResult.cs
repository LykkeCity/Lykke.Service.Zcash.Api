using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Common;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class RecentResult
    {
        public RecentTransaction[] Transactions { get; set; }

        public string LastBlock { get; set; }

        public class RecentTransaction
        {
            public string Address { get; set; }
            public decimal Amount { get; set; }
            public decimal? Fee { get; set; }
            public uint Vout { get; set; }
            public string TxId { get; set; }
            public uint BlockTime { get; set; }
            public long Confirmations { get; set; } // can be -1 for unconfirmed
            public string Category { get; set; }
        }
    }
}
