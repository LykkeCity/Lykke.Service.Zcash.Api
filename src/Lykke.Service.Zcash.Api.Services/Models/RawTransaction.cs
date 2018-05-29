using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class RawTransaction
    {
        public string TxId { get; set; }
        public Input[] Vin { get; set; }
        public Output[] Vout { get; set; }
        public string BlockHash { get; set; }
        public uint BlockTime { get; set; }
        public long Confirmations { get; set; }

        public RawTransactionAction[] GetActions()
        {
            var from = (
                from item in Vin
                from addr in item.Addresses
                group item by addr into g
                select new RawTransactionAction
                {
                    Category = HistoryAddressCategory.From,
                    FromAddress = g.Key,
                    AssetId = Asset.Zec.Id,
                    Amount = g.Sum(v => v.Value)
                }
            ).ToDictionary(
                op => op.AffectedAddress, 
                op => (op, op.Amount));

            var to = (
                from item in Vout
                from addr in item.ScriptPubKey.Addresses
                group item by addr into g
                select new RawTransactionAction
                {
                    Category = HistoryAddressCategory.To,
                    ToAddress = g.Key,
                    AssetId = Asset.Zec.Id,
                    Amount = g.Sum(v => v.Value)
                }
            ).ToDictionary(
                op => op.AffectedAddress, 
                op => (op, op.Amount));

            var fromAddresses = string.Join(',', from.Keys);
            var toAddresses = string.Join(',', to.Keys);

            IEnumerable<RawTransactionAction> Weed(
                Dictionary<string, (RawTransactionAction operation, decimal originalAmount)> left, 
                Dictionary<string, (RawTransactionAction operation, decimal originalAmount)> right, 
                Action<RawTransactionAction> setAddresses)
            {
                foreach (var k in left.Keys)
                {
                    if (right.TryGetValue(k, out var r))
                    {
                        left[k].operation.Amount -= r.originalAmount;
                    }

                    if (left[k].operation.Amount > decimal.Zero)
                    {
                        setAddresses(left[k].operation);
                        yield return left[k].operation;
                    }
                } 
            }

            var fromOperations = Weed(from, to, o => o.ToAddress = toAddresses);
            var toOperations = Weed(to, from, o => o.FromAddress = fromAddresses);

            return fromOperations
                .Concat(toOperations)
                .ToArray();
        }

        public class Input
        {
            public string TxId { get; set; }
            public uint Vout { get; set; }
            public decimal Value { get; set; }
            public string[] Addresses { get; set; }
            public ScriptSig ScriptSig { get; set; }
        }

        public class Output
        {
            public decimal Value { get; set; }
            public uint N { get; set; }
            public ScriptPubKey ScriptPubKey { get; set; }
        }

        public class ScriptPubKey
        {
            public string[] Addresses { get; set; }
        }

        public class ScriptSig
        {
            public string Asm { get; set; }
            public string Hex { get; set; }
        }
    }
}
