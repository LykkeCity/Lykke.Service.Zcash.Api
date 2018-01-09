using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Balances;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IBlockchainService
    {

        /// <summary>
        /// Builds transparent Zcash transaction
        /// </summary>
        /// <param name="operationId">Operation identifier</param>
        /// <param name="from">Sender's t-address</param>
        /// <param name="to">Receiver's t-address</param>
        /// <param name="amount">Amount of money to send</param>
        /// <param name="subtractFees">
        /// If true then fees will be subtracted from <paramref name="amount"/>, otherwise fees will be added to <paramref name="amount"/>
        /// </param>
        /// <returns>Observable transaction</returns>
        Task<ITransaction> BuildUnsignedTxAsync(Guid operationId, BitcoinAddress from, BitcoinAddress to, Money amount, bool subtractFees);

        /// <summary>
        /// Sends transaprent transaction to the Zcash blockchain
        /// </summary>
        /// <param name="tx">Observable transaction</param>
        /// <param name="signedTransaction">Signed transaction in HEX format</param>
        /// <returns></returns>
        Task BroadcastTxAsync(ITransaction tx, string signedTransaction);

        /// <summary>
        /// Returns observable transaction by operation ID, or null if transaction not found.
        /// </summary>
        /// <param name="operationId">Operation identifier</param>
        /// <returns></returns>
        Task<ITransaction> GetObservableTxAsync(Guid operationId);

        /// <summary>
        /// Returns array of observable transactions with specified state.
        /// </summary>
        /// <param name="state">Transaction state to filter transactions</param>
        /// <param name="skip">Count to skip</param>
        /// <param name="take">Count to take</param>
        /// <returns>Read-only list of transactions</returns>
        Task<IReadOnlyList<ITransaction>> GetObservableTxAsync(TransactionState state, int skip = 0, int take = 100);

        /// <summary>
        /// Updates observable transaction state.
        /// </summary>
        /// <param name="tx">Transaction</param>
        /// <returns></returns>
        Task UpdateObservableTxAsync(ITransaction tx);

        /// <summary>
        /// Removes transactions from observation list.
        /// </summary>
        /// <param name="operationIds">Array of operation identifiers</param>
        /// <returns></returns>
        Task DeleteObservableTxAsync(IEnumerable<Guid> operationIds);

        /// <summary>
        /// Returns balances of observable addresses.
        /// </summary>
        /// <param name="skip">Count to skip</param>
        /// <param name="take">Count to take</param>
        /// <returns></returns>
        Task<IReadOnlyList<IBalance>> GetBalancesAsync(int skip = 0, int take = 100);

        /// <summary>
        /// Adds address to observation list.
        /// </summary>
        /// <param name="address">Zcash t-address</param>
        /// <returns></returns>
        Task<bool> CreateObservableAddressAsync(string address);

        /// <summary>
        /// Removes  address from observation list.
        /// </summary>
        /// <param name="address">Zcash t-address</param>
        /// <returns></returns>
        Task<bool> DeleteObservableAddressAsync(string address);

        /// <summary>
        /// Checks if specified address is valid Zcash address
        /// </summary>
        /// <param name="address">Address to check</param>
        /// <param name="bitcoinAddress">Corresponding address representation</param>
        /// <returns>True if address is valid, otherwise false</returns>
        bool ValidateAddress(string address, out BitcoinAddress bitcoinAddress);
    }
}
