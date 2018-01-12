using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
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
        Task<ITransaction> BuildNotSignedTxAsync(Guid operationId, BitcoinAddress from, BitcoinAddress to, Money amount, Asset asset, bool subtractFees);

        /// <summary>
        /// Sends transaprent transaction to the Zcash blockchain
        /// </summary>
        /// <param name="tx">Observable transaction</param>
        /// <param name="transaction">Signed transaction in HEX format</param>
        /// <returns></returns>
        Task BroadcastTxAsync(ITransaction tx, Transaction transaction);

        /// <summary>
        /// Returns observable transaction by operation ID, or null if transaction not found.
        /// </summary>
        /// <param name="operationId">Operation identifier</param>
        /// <returns></returns>
        Task<ITransaction> GetOperationalTxAsync(Guid operationId);

        /// <summary>
        /// Returns array of observable transactions with specified state.
        /// </summary>
        /// <param name="state">Transaction state to filter transactions</param>
        /// <param name="skip">Count to skip</param>
        /// <param name="take">Count to take</param>
        /// <returns>Read-only list of transactions</returns>
        Task<PagedResult<ITransaction>> GetOperationalTxsByStateAsync(TransactionState state, string continuation, int take = 100);

        Task<ITransaction[]> GetHistoryAsync(AddressMonitorType type, string address, string afterHash = null, int take = 100);

        /// <summary>
        /// Updates observable transactions state and adds new transactions to history.
        /// </summary>
        /// <param name="tx">Transaction</param>
        /// <returns></returns>
        Task HandleTxsAsync();

        /// <summary>
        /// Removes transactions from observation list.
        /// </summary>
        /// <param name="operationIds">Array of operation identifiers</param>
        /// <returns></returns>
        Task DeleteOperationalTxsAsync(IEnumerable<Guid> operationIds);

        /// <summary>
        /// Returns balances of observable addresses.
        /// </summary>
        /// <param name="skip">Count to skip</param>
        /// <param name="take">Count to take</param>
        /// <returns></returns>
        Task<PagedResult<AddressBalance>> GetBalancesAsync(string continuation = null, int take = 100);

        /// <summary>
        /// Adds address to observation list.
        /// </summary>
        /// <param name="address">Zcash t-address</param>
        /// <returns></returns>
        Task<bool> TryCreateObservableAddressAsync(AddressMonitorType monitorType, string address);

        /// <summary>
        /// Removes  address from observation list.
        /// </summary>
        /// <param name="address">Zcash t-address</param>
        /// <returns></returns>
        Task<bool> TryDeleteObservableAddressAsync(AddressMonitorType monitorType, string address);

        /// <summary>
        /// Checks if specified address is valid Zcash address
        /// </summary>
        /// <param name="address">Address to check</param>
        /// <param name="bitcoinAddress">Corresponding address representation</param>
        /// <returns>True if address is valid, otherwise false</returns>
        bool ValidateAddress(string address, out BitcoinAddress bitcoinAddress);
    }
}
