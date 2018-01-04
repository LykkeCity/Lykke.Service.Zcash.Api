using Lykke.Service.BlockchainApi.Contract.Responses.PendingEvents;
using Lykke.Service.Zcash.Api.Core.Domain.Events;
using Lykke.Service.Zcash.Api.Models;

namespace Lykke.Service.BlockchainApi.Contract.Responses
{
    public static class PendingEventExtensions
    {
        public static PendingEventContract ToContract(this IPendingEvent self)
        {
            return new PendingEventContract
            {
                Amount = self.Amount,
                AssetId = self.AssetId,
                FromAddress = self.FromAddress,
                OperationId = self.OperationId,
                Timestamp = self.CreatedUtc,
                ToAddress = self.ToAddress,
                TransactionHash = self.TransactionHash,
            };
        }

        public static PendingCashinEventContract ToCashin(this IPendingEvent self)
        {
            return new PendingCashinEventContract
            {
                Address = self.FromAddress,
                Amount = self.Amount,
                AssetId = self.AssetId,
                OperationId = self.OperationId,
                Timestamp = self.CreatedUtc
            };
        }

        public static PendingCashoutCompletedEventContract ToCashoutCompleted(this IPendingEvent self)
        {
            return new PendingCashoutCompletedEventContract
            {
                Amount = self.Amount,
                AssetId = self.AssetId,
                FromAddress = self.FromAddress,
                OperationId = self.OperationId,
                Timestamp = self.CreatedUtc,
                ToAddress = self.ToAddress,
                TransactionHash = self.TransactionHash
            };
        }

        public static PendingCashoutFailedEventContract ToCashoutFailed(this IPendingEvent self)
        {
            return new PendingCashoutFailedEventContract
            {
                Amount = self.Amount,
                AssetId = self.AssetId,
                FromAddress = self.FromAddress,
                OperationId = self.OperationId,
                Timestamp = self.CreatedUtc,
                ToAddress = self.ToAddress
            };
        }

        public static PendingCashoutStartedEventContract ToCashoutStarted(this IPendingEvent self)
        {
            return new PendingCashoutStartedEventContract
            {
                Amount = self.Amount,
                AssetId = self.AssetId,
                FromAddress = self.FromAddress,
                OperationId = self.OperationId,
                Timestamp = self.CreatedUtc,
                ToAddress = self.ToAddress,
                TransactionHash = self.TransactionHash,
            };
        }
    }
}
