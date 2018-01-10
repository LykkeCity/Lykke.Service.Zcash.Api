using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Insight;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IInsightClient
    {
        /// <summary>
        /// Returns unspent transaction outputs for Zcash t-address
        /// </summary>
        /// <param name="address">Transaparent Zcash address</param>
        /// <returns>Array of UTXO objects</returns>
        Task<Utxo[]> GetUtxoAsync(params BitcoinAddress[] address);

        /// <summary>
        /// Sends transaperent transaction to the Zcash blockchain
        /// </summary>
        /// <param name="rawTxHex">Transaparent transaction</param>
        /// <returns>Transaction hash</returns>
        Task<SendTransactionResult> SendTransactionAsync(string rawTxHex);
    }
}
