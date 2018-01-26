using System;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public static class OperationExtensions
    {
        public static BroadcastedSingleTransactionResponse ToSingleResponse(this IOperation self)
        {
            return new BroadcastedSingleTransactionResponse
            {
                Amount = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Fee = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Error = self.Error,
                Hash = self.Hash,
                OperationId = self.OperationId,
                State = self.State.ToBroadcastedState(),
                Timestamp = (self.SentUtc ?? self.CompletedUtc ?? self.FailedUtc).Value,
                
            };
        }

        public static BroadcastedTransactionWithManyInputsResponse ToManyInputsResponse(this IOperation self)
        {
            return new BroadcastedTransactionWithManyInputsResponse
            {
                Amount = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Fee = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Error = self.Error,
                Hash = self.Hash,
                OperationId = self.OperationId,
                State = self.State.ToBroadcastedState(),
                Timestamp = (self.SentUtc ?? self.CompletedUtc ?? self.FailedUtc).Value
            };
        }

        public static BroadcastedTransactionWithManyOutputsResponse ToManyOutputsResponse(this IOperation self)
        {
            return new BroadcastedTransactionWithManyOutputsResponse
            {
                Amount = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Fee = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Error = self.Error,
                Hash = self.Hash,
                OperationId = self.OperationId,
                State = self.State.ToBroadcastedState(),
                Timestamp = (self.SentUtc ?? self.CompletedUtc ?? self.FailedUtc).Value
            };
        }

        public static BroadcastedTransactionState ToBroadcastedState(this OperationState self)
        {
            return (BroadcastedTransactionState)((int)self + 1);
        }
    }
}
