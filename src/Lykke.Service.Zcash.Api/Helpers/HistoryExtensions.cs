using System;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain.History;

namespace Lykke.Service.Zcash.Api.Helpers
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
                Timestamp = self.TimestampUtc,
                ToAddress = self.ToAddress
            };
        }
    }
}
