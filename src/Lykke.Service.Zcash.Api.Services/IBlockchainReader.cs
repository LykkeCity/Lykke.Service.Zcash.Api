using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Services.Models;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Services
{
    public interface IBlockchainReader
    {
        Task<RawTransaction> GetRawTransactionAsync(string hash);
        Task<Utxo[]> ListUnspentAsync(int confirmationLevel, params string[] addresses);
        Task ImportAddressAsync(string address);
        Task<RecentResult> ListSinceBlockAsync(string lastBlockHash, int confirmationLevel);
        Task<string> SendRawTransactionAsync(Transaction transaction);
        Task<AddressInfo> ValidateAddressAsync(string address);
        Task<string[]> GetAddresssesAsync();
    }
}
