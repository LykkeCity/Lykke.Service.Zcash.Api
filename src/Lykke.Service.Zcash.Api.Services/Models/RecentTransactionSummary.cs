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

            // due to absence of "from" address(es) we can receive multiple descriptors differing by amounts only,
            // i.e. for transfers from different addresses to a single deposit wallet within a single transaction;
            // so to be able to store and distinguish operations in history we group
            // descriptors by category (wallet relatively), "to" address and tx hash:

            return from t in Transactions
                   where t.Confirmations >= requiredConfirmations && (t.Category == send || t.Category == receive)
                   group t by new { t.Address, t.Category, t.TxId } into g
                   let time = g.First().BlockTime
                   select new RecentTransaction
                   {
                       Amount = Math.Abs(g.Sum(e => e.Amount)),
                       AssetId = Asset.Zec.Id,
                       BlockTime = time,
                       Hash = g.Key.TxId,
                       ObservationSubject = g.Key.Category == send ?
                           ObservationSubject.From :
                           ObservationSubject.To,
                       TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime,
                       ToAddress = g.Key.Address
                   };
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
