using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class RawTransaction
    {
        public string TxId { get; set; }
        public Input[] Vin { get; set; }
        public Output[] Vout { get; set; }

        public RawTransactionOperation[] GetOperations()
        {
            var from = (
                from item in Vin
                from addr in item.Addresses
                group item by addr into g
                select new RawTransactionOperation
                {
                    Category = Constants.TransactionOperations.Send,
                    AffectedAddress = g.Key,
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
                select new RawTransactionOperation
                {
                    Category = Constants.TransactionOperations.Receive,
                    AffectedAddress = g.Key,
                    ToAddress = g.Key,
                    AssetId = Asset.Zec.Id,
                    Amount = g.Sum(v => v.Value)
                }
            ).ToDictionary(
                op => op.AffectedAddress, 
                op => (op, op.Amount));

            var fromAddresses = string.Join(',', from.Keys);
            var toAddresses = string.Join(',', to.Keys);

            IEnumerable<RawTransactionOperation> Weed(
                Dictionary<string, (RawTransactionOperation operation, decimal originalAmount)> left, 
                Dictionary<string, (RawTransactionOperation operation, decimal originalAmount)> right, 
                Action<RawTransactionOperation> setAddresses)
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
    }
}
