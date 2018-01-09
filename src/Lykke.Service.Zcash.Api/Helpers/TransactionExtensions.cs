using Lykke.Service.BlockchainApi.Contract.Transactions;

namespace Lykke.Service.Zcash.Api.Core.Domain.Transactions
{
    public static class TransactionExtensions
    {
        public static InProgressTransactionContract ToInProgressContract(this ITransaction self)
        {
            return new InProgressTransactionContract
            {
                Amount = self.Amount,
                AssetId = self.AssetId,
                FromAddress = self.FromAddress,
                Hash = self.Hash,
                OperationId = self.OperationId,
                ToAddress = self.ToAddress,
                Timestamp = self.SentUtc.Value
            };
        }

        public static CompletedTransactionContract ToCompletedContract(this ITransaction self)
        {
            return new CompletedTransactionContract
            {
                Amount = self.Amount,
                AssetId = self.AssetId,
                FromAddress = self.FromAddress,
                OperationId = self.OperationId,
                Timestamp = self.CompletedUtc.Value,
                ToAddress = self.ToAddress,
                Hash = self.Hash
            };
        }

        public static FailedTransactionContract ToFailedContract(this ITransaction self)
        {
            return new FailedTransactionContract
            {
                Amount = self.Amount,
                AssetId = self.AssetId,
                FromAddress = self.FromAddress,
                OperationId = self.OperationId,
                Timestamp = self.FailedUtc.Value,
                ToAddress = self.ToAddress
            };
        }
    }
}
