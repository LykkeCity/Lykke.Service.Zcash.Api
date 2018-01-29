using System;
using System.Linq;
using Common;
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
                Timestamp = self.TimestampUtc,
                Block = Convert.ToInt64(self.TimestampUtc.ToUnixTime()),
            };
        }

        public static BroadcastedTransactionWithManyInputsResponse ToManyInputsResponse(this IOperation self)
        {
            return new BroadcastedTransactionWithManyInputsResponse
            {
                Error = self.Error,
                Hash = self.Hash,
                OperationId = self.OperationId,
                State = self.State.ToBroadcastedState(),
                Timestamp = self.TimestampUtc,
                Block = Convert.ToInt64(self.TimestampUtc.ToUnixTime()),
                Fee = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Inputs = self.Items
                    .Select(x => new TransactionInputContract { Amount = Conversions.CoinsToContract(x.Amount, Constants.Assets[self.AssetId].DecimalPlaces), FromAddress = x.FromAddress })
                    .ToArray()
            };
        }

        public static BroadcastedTransactionWithManyOutputsResponse ToManyOutputsResponse(this IOperation self)
        {
            return new BroadcastedTransactionWithManyOutputsResponse
            {
                Error = self.Error,
                Hash = self.Hash,
                OperationId = self.OperationId,
                State = self.State.ToBroadcastedState(),
                Timestamp = (self.SentUtc ?? self.CompletedUtc ?? self.FailedUtc).Value,
                Block = Convert.ToInt64(self.TimestampUtc.ToUnixTime()),
                Fee = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Outputs = self.Items
                    .Select(x => new TransactionOutputContract { Amount = Conversions.CoinsToContract(x.Amount, Constants.Assets[self.AssetId].DecimalPlaces), ToAddress = x.ToAddress })
                    .ToArray()
            };
        }

        public static BroadcastedTransactionState ToBroadcastedState(this OperationState self)
        {
            return (BroadcastedTransactionState)((int)self + 1);
        }
    }
}
