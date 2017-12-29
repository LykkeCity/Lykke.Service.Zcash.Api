using System;
using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IBlockchainService
    {
        Task<string> CreateTransparentWalletAsync();

        Task Transfer(BitcoinAddress from, BitcoinAddress to, Money amount, params BitcoinAddress[] signers);

        bool IsValidAddress(string address);
    }
}
