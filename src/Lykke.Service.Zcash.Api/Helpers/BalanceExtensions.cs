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
                Address = self.Address.ToString(),
                AssetId = self.Asset.Id,
                Balance = Conversions.CoinsToContract(self.Balance.ToUnit(self.Asset.Unit), self.Asset.DecimalPlaces)
            };
        }
    }
}
