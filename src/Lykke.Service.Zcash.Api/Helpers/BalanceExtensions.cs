using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Balances;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Domain.Addresses
{
    public static class BalanceExtensions
    {
        public static WalletBalanceContract ToWalletContract(this AddressBalance self)
        {
            return new WalletBalanceContract()
            {
                Address = self.Address,
                AssetId = self.AssetId,
                Balance = Conversions.CoinsToContract(self.Balance, Constants.Assets[self.AssetId].DecimalPlaces)
            };
        }
    }
}
