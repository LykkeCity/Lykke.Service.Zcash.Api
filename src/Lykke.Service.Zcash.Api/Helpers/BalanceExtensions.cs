using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Balances;

namespace Lykke.Service.Zcash.Api.Core.Domain.Balances
{
    public static class BalanceExtensions
    {
        public static WalletBalanceContract ToWalletBalanceContract(this IBalance self)
        {
            return new WalletBalanceContract()
            {
                Address = self.Address,
                AssetId = self.Asset.Id,
                Balance = Conversions.CoinsToContract(self.Balance.ToUnit(self.Asset.Unit), self.Asset.DecimalPlaces)
            };
        }
    }
}
