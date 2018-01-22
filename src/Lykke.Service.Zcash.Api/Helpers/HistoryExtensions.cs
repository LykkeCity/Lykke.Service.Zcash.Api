using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;

namespace Lykke.Service.Zcash.Api.Core.Domain.History
{
    public static class HistoryExtensions
    {
        public static HistoricalTransactionContract ToHistoricalContract(this IHistoryItem self)
        {
            return new HistoricalTransactionContract
            {
                Amount = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                AssetId = self.AssetId,
                FromAddress = self.FromAddress,
                Hash = self.Hash,
                OperationId = self.OperationId ?? Guid.Empty,
                Timestamp = self.TimestampUtc,
                ToAddress = self.ToAddress
            };
        }
    }
}
