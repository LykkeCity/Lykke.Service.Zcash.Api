using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IBlockchainService
    {
        /// <summary>
        /// Generates new t-address for Zcash
        /// </summary>
        /// <returns>Public address</returns>
        Task<string> CreateTransparentWalletAsync();

        /// <summary>
        /// Builds, signs and sends transparent Zcash transaction
        /// </summary>
        /// <param name="from">Sender's address</param>
        /// <param name="to">Receiver's address</param>
        /// <param name="amount">Amount of money to send</param>
        /// <param name="signers">Additional addresses to sign the transaction</param>
        /// <returns>Transaction hash</returns>
        Task<string> TransferAsync(BitcoinAddress from, IDestination to, Money amount, params BitcoinAddress[] signers);

        /// <summary>
        /// Checks if specified address is valid Zcash address
        /// </summary>
        /// <param name="address">Address to check</param>
        /// <param name="bitcoinAddress">Corresponding address representation</param>
        /// <returns>True if address is valid, otherwise false</returns>
        bool IsValidAddress(string address, out BitcoinAddress bitcoinAddress);
    }
}
