using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class RecentTransactionSummary
    {
        public Transaction[] Transactions { get; set; }

        public string LastBlock { get; set; }

        public IEnumerable<RecentTransaction> GetConfirmed(int requiredConfirmations)
        {
            const string receive = "receive";
            const string send = "send";

            return Transactions
                .Where(t => t.Category == send || t.Category == receive)
                .Where(t => t.Confirmations >= requiredConfirmations)
                .GroupBy(t => new { t.Category, t.Address, t.TxId, t.BlockTime })
                .Select(g => new RecentTransaction
                {
                    Amount = Math.Abs(g.Sum(e => e.Amount)),
                    AssetId = Asset.Zec.Id,
                    Hash = g.Key.TxId,
                    ObservationSubject = g.Key.Category == send 
                        ? ObservationSubject.From 
                        : ObservationSubject.To,
                    TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(g.Key.BlockTime).UtcDateTime,
                    ToAddress = g.Key.Address
                });
        }

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
