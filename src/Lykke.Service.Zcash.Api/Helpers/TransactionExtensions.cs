using System;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public static class TransactionExtensions
    {
        public static BroadcastedTransactionResponse ToBroadcastedResponse(this IOperationalTransaction self)
        {
            return new BroadcastedTransactionResponse
            {
                Amount = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Fee = self.Fee.HasValue 
                    ? Conversions.CoinsToContract(self.Fee.Value, Constants.Assets[self.AssetId].DecimalPlaces) 
                    : null,
                Error = self.Error,
                Hash = self.Hash,
                OperationId = self.OperationId,
                State = self.State.ToBroadcastedState(),
                Timestamp = (self.SentUtc ?? self.CompletedUtc ?? self.FailedUtc).Value
            };
        }

        public static BroadcastedTransactionState ToBroadcastedState(this TransactionState self)
        {
            return (BroadcastedTransactionState)((int)self + 1);
        }

        public static HistoricalTransactionContract ToHistoricalContract(this ITransaction self)
        {
            return new HistoricalTransactionContract
            {
                Amount = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                AssetId = self.AssetId,
                FromAddress = self.FromAddress,
                Hash = self.Hash,
                OperationId = self.OperationId,
                Timestamp = self.TimestampUtc,
                ToAddress = self.ToAddress
            };
        }
    }
}
