using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;
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
        Task<IOperationalTransaction> BuildNotSignedTxAsync(Guid operationId, BitcoinAddress from, BitcoinAddress to, Money amount, Asset asset, bool subtractFees);

        /// <summary>
        /// Sends transaprent transaction to the Zcash blockchain
        /// </summary>
        /// <param name="tx">Observable transaction</param>
        /// <param name="transaction">Signed transaction in HEX format</param>
        /// <returns></returns>
        Task BroadcastTxAsync(IOperationalTransaction tx, string transaction);

        /// <summary>
        /// Returns observable transaction by operation ID, or null if transaction not found.
        /// </summary>
        /// <param name="operationId">Operation identifier</param>
        /// <returns></returns>
        Task<IOperationalTransaction> GetOperationalTxAsync(Guid operationId);

        /// <summary>
        /// Removes transactions from observation list.
        /// </summary>
        /// <param name="operationIds">Array of operation identifiers</param>
        /// <returns>True, if tx successfully deleted, false if tx is not observed</returns>
        Task<bool> TryDeleteOperationalTxAsync(Guid operationId);

        /// <summary>
        /// Updates observable transactions state and adds new transactions to history.
        /// </summary>
        /// <param name="tx">Transaction</param>
        /// <returns></returns>
        Task HandleHistoryAsync();

        /// <summary>
        /// Returns historical transaction data for specified address.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="address">Address</param>
        /// <param name="afterHash">Method returns transactions after transaction with specified hash</param>
        /// <param name="take">Count of transactions to return</param>
        /// <returns></returns>
        Task<IEnumerable<ITransaction>> GetHistoryAsync(ObservationSubject type, string address, string afterHash = null, int take = 100);

        /// <summary>
        /// Returns balances of observable addresses.
        /// </summary>
        /// <param name="skip">Count to skip</param>
        /// <param name="take">Count to take</param>
        /// <returns></returns>
        Task<(string continuation, IEnumerable<AddressBalance> items)> GetBalancesAsync(string continuation = null, int take = 100);

        /// <summary>
        /// Adds address to observation list.
        /// </summary>
        /// <param name="address">Zcash t-address</param>
        /// <returns>True, if address successfully created, false if address is already observed</returns>
        Task<bool> TryCreateObservableAddressAsync(ObservationSubject subject, string address);

        /// <summary>
        /// Removes  address from observation list.
        /// </summary>
        /// <param name="address">Zcash t-address</param>
        /// <returns>True, if address successfully deleted, false if address is not observed</returns>
        Task<bool> TryDeleteObservableAddressAsync(ObservationSubject subject, string address);

        /// <summary>
        /// Checks if specified address is valid Zcash address
        /// </summary>
        /// <param name="address">Address to check</param>
        /// <param name="bitcoinAddress">Corresponding address representation</param>
        /// <returns>True if address is valid, otherwise false</returns>
        bool ValidateAddress(string address, out BitcoinAddress bitcoinAddress);
    }
}
