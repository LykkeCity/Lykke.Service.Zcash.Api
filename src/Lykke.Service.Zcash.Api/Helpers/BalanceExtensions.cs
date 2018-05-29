using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;

namespace Lykke.Service.Zcash.Api.Helpers
{
    public static class BalanceExtensions
    {
        public static WalletBalanceContract ToWalletContract(this AddressBalance self)
        {
            return new WalletBalanceContract()
            {
                Address = self.Address,
                AssetId = self.Asset.Id,
                Balance = Conversions.CoinsToContract(self.Balance, self.Asset.DecimalPlaces),
                Block = self.BlockTime
            };
        }
    }
}
