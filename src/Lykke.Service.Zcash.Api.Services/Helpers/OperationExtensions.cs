﻿using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Services.Models;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public static class OperationExtensions
    {
        public static RawTransactionAction[] GetRawTransactionActions(this IOperation self)
        {
            return self.Items
                .SelectMany(item =>
                {
                    return new RawTransactionAction[]
                    {
                        new RawTransactionAction
                        {
                            Category = HistoryAddressCategory.From,
                            Amount = item.Amount,
                            AssetId = self.AssetId,
                            FromAddress = item.FromAddress,
                            ToAddress = item.ToAddress
                        },
                        new RawTransactionAction
                        {
                            Category = HistoryAddressCategory.To,
                            Amount = item.Amount,
                            AssetId = self.AssetId,
                            FromAddress = item.FromAddress,
                            ToAddress = item.ToAddress
                        },
                    };
                })
                .ToArray();
        }
    }
}
