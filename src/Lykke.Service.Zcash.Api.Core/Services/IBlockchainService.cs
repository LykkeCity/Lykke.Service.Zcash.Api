using System;
using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IBlockchainService
    {
        Task<string> CreateTransparentWalletAsync();

        Task TransferAsync(BitcoinAddress from, IDestination to, Money amount, params BitcoinAddress[] signers);

        bool IsValidAddress(string address, out BitcoinAddress bitcoinAddress);
    }
}
