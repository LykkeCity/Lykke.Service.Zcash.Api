using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Zcash.Api.Services.Models;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public static class OperationExtensions
    {
        public static RawTransactionOperation[] GetTransactionOperations(this IOperation self)
        {
            return self.Items
                .SelectMany(item =>
                {
                    return new RawTransactionOperation[]
                    {
                        new RawTransactionOperation
                        {
                            Category = Constants.TransactionOperations.Send,
                            AffectedAddress = item.FromAddress,
                            Amount = item.Amount,
                            AssetId = item.AssetId,
                            FromAddress = item.FromAddress,
                            ToAddress = item.ToAddress
                        },
                        new RawTransactionOperation
                        {
                            Category = Constants.TransactionOperations.Receive,
                            AffectedAddress = item.ToAddress,
                            Amount = item.Amount,
                            AssetId = item.AssetId,
                            FromAddress = item.FromAddress,
                            ToAddress = item.ToAddress
                        },
                    };
                })
                .ToArray();
        }
    }
}
