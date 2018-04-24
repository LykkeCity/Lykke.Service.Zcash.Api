using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Services.Models;

namespace Lykke.Service.Zcash.Api.Services
{
    public interface IBlockchainReader
    {
        Task<RawTransaction> GetRawTransactionAsync(string hash);
        Task<Utxo[]> ListUnspentAsync(int confirmationLevel, params string[] addresses);
        Task ImportAddressAsync(string address);
        Task<RecentResult> ListSinceBlockAsync(string lastBlockHash, int confirmationLevel);
        Task<string> SendRawTransactionAsync(string transaction);
        Task<AddressInfo> ValidateAddressAsync(string address);
        Task<string[]> GetAddresssesAsync();
        Task<Info> GetInfoAsync();
        Task<string> CreateRawTransaction(Utxo[] inputs, Dictionary<string, decimal> outputs);
        Task<RawTransaction> DecodeRawTransaction(string transaction);
        Task<Block> GetBlockAsync(string blockHash);
    }
}
