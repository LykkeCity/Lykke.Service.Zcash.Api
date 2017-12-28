using System;
using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IWalletService
    {
        Task<string> CreateTransparentWalletAsync();

        //Task<Guid> Cashout(BitcoinAddress from, BitcoinAddress to, Money amount, BitcoinAddress[] signers);

        bool IsValid(string address);
    }
}
