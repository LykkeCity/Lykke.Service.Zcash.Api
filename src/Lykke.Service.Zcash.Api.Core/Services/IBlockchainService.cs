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
        /// Builds transparent Zcash transaction
        /// </summary>
        /// <param name="from">Sender's t-address</param>
        /// <param name="to">Receiver's t-address</param>
        /// <param name="amount">Amount of money to send</param>
        /// <param name="subtractFees">
        /// If true then fees will be subtracted from <paramref name="amount"/>, otherwise fees will be added to <paramref name="amount"/>
        /// </param>
        /// <returns>Transaction data, serialized to JSON</returns>
        Task<string> BuildTransactionAsync(BitcoinAddress from, BitcoinAddress to, Money amount,
            bool subtractFees = false,
            decimal feeFactor = decimal.One);

        /// <summary>
        /// Sends transaprent transaction to the Zcash blockchain
        /// </summary>
        /// <param name="tx">T-transaction</param>
        /// <returns>Transaction hash</returns>
        Task<string> BroadcastTransactionAsync(Transaction tx);

        /// <summary>
        /// Checks if specified address is valid Zcash address
        /// </summary>
        /// <param name="address">Address to check</param>
        /// <param name="bitcoinAddress">Corresponding address representation</param>
        /// <returns>True if address is valid, otherwise false</returns>
        bool IsValidAddress(string address, out BitcoinAddress bitcoinAddress);
    }
}
