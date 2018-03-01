using System;
using System.Linq;
using Common;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public static class OperationExtensions
    {
        public static BroadcastedSingleTransactionResponse ToSingleResponse(this IOperation self)
        {
            self.EnsureType(OperationType.SingleFromSingleTo);

            return new BroadcastedSingleTransactionResponse
            {
                Amount = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Fee = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Error = self.Error,
                Hash = self.Hash,
                OperationId = self.OperationId,
                State = self.State.ToBroadcastedState(),
                Timestamp = self.TimestampUtc,
                Block = new DateTimeOffset(self.TimestampUtc).ToUnixTimeSeconds(),
            };
        }

        public static BroadcastedTransactionWithManyInputsResponse ToManyInputsResponse(this IOperation self)
        {
            self.EnsureType(OperationType.MultiFromSingleTo);

            return new BroadcastedTransactionWithManyInputsResponse
            {
                Error = self.Error,
                Hash = self.Hash,
                OperationId = self.OperationId,
                State = self.State.ToBroadcastedState(),
                Timestamp = self.TimestampUtc,
                Block = new DateTimeOffset(self.TimestampUtc).ToUnixTimeSeconds(),
                Fee = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Inputs = self.Items
                    .Select(x => new BroadcastedTransactionInputContract { Amount = Conversions.CoinsToContract(x.Amount, Constants.Assets[self.AssetId].DecimalPlaces), FromAddress = x.FromAddress })
                    .ToArray()
            };
        }

        public static BroadcastedTransactionWithManyOutputsResponse ToManyOutputsResponse(this IOperation self)
        {
            self.EnsureType(OperationType.SingleFromMultiTo);

            return new BroadcastedTransactionWithManyOutputsResponse
            {
                Error = self.Error,
                Hash = self.Hash,
                OperationId = self.OperationId,
                State = self.State.ToBroadcastedState(),
                Timestamp = (self.SentUtc ?? self.CompletedUtc ?? self.FailedUtc).Value,
                Block = new DateTimeOffset(self.TimestampUtc).ToUnixTimeSeconds(),
                Fee = Conversions.CoinsToContract(self.Amount, Constants.Assets[self.AssetId].DecimalPlaces),
                Outputs = self.Items
                    .Select(x => new BroadcastedTransactionOutputContract { Amount = Conversions.CoinsToContract(x.Amount, Constants.Assets[self.AssetId].DecimalPlaces), ToAddress = x.ToAddress })
                    .ToArray()
            };
        }

        public static BroadcastedTransactionState ToBroadcastedState(this OperationState self)
        {
            return
                self == OperationState.Completed ? BroadcastedTransactionState.Completed :
                self == OperationState.Failed ? BroadcastedTransactionState.Failed :
                self == OperationState.Sent ? BroadcastedTransactionState.InProgress :
                throw new InvalidOperationException($"Operation is {Enum.GetName(typeof(OperationState), self)}, it should not be requested through API.");
        }

        public static void EnsureType(this IOperation self, OperationType expected)
        {
            if (self.Type != expected)
            {
                throw new InvalidOperationException($"{expected} operation type was expected, {self.Type} operation was fetched");
            }
        }
    }
}
