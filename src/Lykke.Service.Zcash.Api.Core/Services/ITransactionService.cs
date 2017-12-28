using System;
using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface ITransactionService
    {
        Task<Guid> Transfer(BitcoinAddress from, BitcoinAddress to, Money amount, params BitcoinAddress[] signers);
    }
}
