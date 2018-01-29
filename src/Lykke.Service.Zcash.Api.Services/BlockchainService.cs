﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.History;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;
using Lykke.Service.Zcash.Api.Core.Domain.Settings;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Zcash.Api.Services.Models;
using NBitcoin;
using NBitcoin.JsonConverters;
using NBitcoin.Policy;
using NBitcoin.RPC;
using NBitcoin.Zcash;
using Newtonsoft.Json;

namespace Lykke.Service.Zcash.Api.Services
{
    public class BlockchainService : IBlockchainService
    {
        private readonly ILog _log;
        private readonly RPCClient _rpcClient;
        private readonly IAddressRepository _addressRepository;
        private readonly IOperationRepository _operationRepository;
        private readonly IHistoryRepository _historyRepository;
        private readonly ISettingsRepository _settingsRepository;
        private readonly ZcashApiSettings _settings;

        public BlockchainService(
            ILog log,
            RPCClient rpcClient,
            IAddressRepository addressRepository,
            IOperationRepository operationRepository,
            IHistoryRepository historyRepository,
            ISettingsRepository settingsRepository,
            ZcashApiSettings settings)
        {
            _log = log;
            _rpcClient = rpcClient;
            _addressRepository = addressRepository;
            _operationRepository = operationRepository;
            _historyRepository = historyRepository;
            _settingsRepository = settingsRepository;
            _settings = settings;
        }

        public bool ValidateAddress(string address, out BitcoinAddress bitcoinAddress)
        {
            try
            {
                bitcoinAddress = BitcoinAddress.Create(address);
                return bitcoinAddress != null;
            }
            catch
            {
                bitcoinAddress = null;
                return false;
            }
        }

        public void EnsureSigned(Transaction transaction, ICoin[] coins)
        {
            // check transaction sign only (due to differences between BTC and ZEC fee calculation
            if (!new TransactionBuilder()
                .SetTransactionPolicy(new StandardTransactionPolicy { CheckFee = false })
                .Verify(transaction, out var errors))
            {
                throw new InvalidOperationException(errors.ToStringViaSeparator(Environment.NewLine));
            }
        }

        public async Task<string> BuildAsync(Guid operationId, OperationType type, (BitcoinAddress from, BitcoinAddress to, Money amount)[] items, Asset asset, bool subtractFee)
        {
            var settings = await LoadStoredSettingsAsync();
            var itemGroups = items.GroupBy(x => x.from.ToString());
            var utxo = await SendRpcAsync<Utxo[]>(RPCOperations.listunspent, 
                settings.ConfirmationLevel, int.MaxValue, itemGroups.Select(g => g.Key).ToArray());
            var transactionBuilder = new TransactionBuilder();

            foreach (var group in itemGroups)
            {
                transactionBuilder.Then(group.Key)
                    .AddCoins(utxo.Where(x => x.Address == group.Key).Select(x => x.AsCoin()))
                    .SetChange(group.First().from);

                foreach (var opItem in group)
                {
                    transactionBuilder.Send(opItem.to, opItem.amount);
                }
            }

            var fee = CalcFee(transactionBuilder, settings);

            transactionBuilder.SendFeesSplit(fee);

            if (subtractFee)
            {
                transactionBuilder.SubtractFees();
            }

            Transaction tx = null;

            try
            {
                tx = transactionBuilder.BuildTransaction(sign: false);
            }
            catch (NotEnoughFundsException)
            {
                throw;
            }

            await _operationRepository.UpsertAsync(operationId, type,
                items.Select(x => (x.from.ToString(), x.to.ToString(), x.amount.ToUnit(asset.Unit))).ToArray(),
                fee.ToUnit(asset.Unit),
                subtractFee,
                asset.Id);

            return Serializer.ToString((tx, transactionBuilder.FindSpentCoins(tx)));
        }

        public async Task BroadcastAsync(Guid operationId, Transaction transaction)
        {
            var rpc = await SendRpcAsync(RPCOperations.sendrawtransaction, transaction.ToHex());

            await _operationRepository.UpdateAsync(operationId, sentUtc: DateTime.UtcNow, hash: rpc.ResultString);
        }

        public async Task<IOperation> GetOperationAsync(Guid operationId, bool loadItems = true)
        {
            return await _operationRepository.GetAsync(operationId, loadItems);
        }

        public async Task<bool> TryDeleteOperationAsync(Guid operationId)
        {
            var operation = await _operationRepository.GetAsync(operationId, false);
            if (operation.State != OperationState.Deleted)
            {
                await _operationRepository.UpdateAsync(operationId, deletedUtc: DateTime.UtcNow);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task HandleHistoryAsync()
        {
            const string receiveCategory = "receive";
            const string sendCategory = "send";

            var settings = await LoadStoredSettingsAsync();

            var recent = await SendRpcAsync<RecentResult>(RPCOperations.listsinceblock,
                settings.LastBlockHash, 
                settings.ConfirmationLevel, 
                true);

            var recentTransactions = from t in recent.Transactions
                                     where t.Category == receiveCategory || t.Category == sendCategory
                                     where t.Confirmations >= settings.ConfirmationLevel
                                     group t by t.TxId into g
                                     select new
                                     {
                                         TimestampUtc = g.First().BlockTime.FromUnixDateTime(),
                                         Hash = g.Key,
                                     };

            var recorded = 0;

            foreach (var transaction in recentTransactions)
            {
                var transactionActions = new RawTransactionAction[0];

                var operation = await _operationRepository.GetAsync(transaction.Hash);

                if (operation != null)
                {
                    await _operationRepository.UpdateAsync(operation.OperationId, completedUtc: transaction.TimestampUtc);

                    transactionActions = operation.GetRawTransactionActions();
                }
                else
                {
                    transactionActions = (await GetRawTransactionAsync(transaction.Hash)).GetActions();
                }

                foreach (var action in transactionActions)
                {
                    if (await IsObservableAsync(action.Category, action.AffectedAddress))
                    {
                        await _historyRepository.UpsertAsync(action.Category, action.AffectedAddress, transaction.TimestampUtc, transaction.Hash,
                            operation?.OperationId, action.FromAddress, action.ToAddress, action.Amount, action.AssetId);

                        recorded++;
                    }
                }
            }

            await SaveStoredSettingsAsync(recent.LastBlock);

            await _log.WriteInfoAsync(nameof(HandleHistoryAsync), 
                $"Range: [{settings.LastBlockHash} - {recent.LastBlock}]",
                $"History handled. {recorded} of {recent.Transactions.Length} recorded");
        }

        public async Task<IEnumerable<IHistoryItem>> GetHistoryAsync(ObservationCategory category, string address, string afterHash = null, int take = 100)
        {
            return await _historyRepository.GetByAddressAsync(category, address, afterHash, take);
        }

        public async Task<(string continuation, IEnumerable<AddressBalance> items)> GetBalancesAsync(string continuation = null, int take = 100)
        {
            var settings = await LoadStoredSettingsAsync();
            var balances = new List<AddressBalance>();
            var addressQuery = await _addressRepository.GetByCategoryAsync(ObservationCategory.Balance, continuation, take);

            if (addressQuery.items.Any())
            {
                var utxo = await SendRpcAsync<Utxo[]>(RPCOperations.listunspent,
                    settings.ConfirmationLevel, int.MaxValue, addressQuery.items.Select(x => x.Address).ToArray());                    

                foreach (var group in utxo.GroupBy(x => x.Address))
                {
                    var lastTx = await GetRawTransactionAsync(
                        group.OrderByDescending(x => x.Confirmations).First().TxId, 
                        restoreInputs: false);

                    balances.Add(new AddressBalance
                    {
                        Address = group.Key,
                        Balance = group.Sum(x => x.Amount),
                        Asset = Asset.Zec,
                        BlockTime = lastTx.BlockTime
                    });
                }
            }

            return (addressQuery.continuation, balances);
        }

        public async Task<bool> TryCreateObservableAddressAsync(ObservationCategory category, string address)
        {
            var addressInfo = await SendRpcAsync<AddressInfo>(RPCOperations.validateaddress, address);

            if (!addressInfo.IsValid)
            {
                throw new InvalidOperationException($"Invalid Zcash address: {address}");
            }

            if (!addressInfo.IsMine && !addressInfo.IsWatchOnly)
            {
                await SendRpcAsync(RPCOperations.importaddress, address, string.Empty, false);
            }

            var observableAddress = await _addressRepository.GetAsync(category, address);

            if (observableAddress == null)
            {
                await _addressRepository.CreateAsync(category, address);
                return true;
            }

            return false;
        }

        public async Task<bool> TryDeleteObservableAddressAsync(ObservationCategory category, string address)
        {
            var observableAddress = await _addressRepository.GetAsync(category, address);

            if (observableAddress != null)
            {
                await _addressRepository.DeleteAsync(category, address);
                return true;
            }

            return false;
        }

        public async Task<bool> IsObservableAsync(ObservationCategory category, string address)
        {
            return (await _addressRepository.GetAsync(category, address)) != null;
        }

        public async Task<ISettings> LoadStoredSettingsAsync()
        {
            return (await _settingsRepository.GetAsync()) ?? _settings;
        }

        public async Task SaveStoredSettingsAsync(string blockHash)
        {
            _settings.LastBlockHash = blockHash;

            await _settingsRepository.UpsertAsync(_settings);
        }

        public async Task<RawTransaction> GetRawTransactionAsync(string transactionHash, bool restoreInputs = true)
        {
            async Task<RawTransaction> InternalGet(string hash)
            {
                var tx = await SendRpcAsync<RawTransaction>(RPCOperations.getrawtransaction, hash, 1);
                if (tx == null)
                {
                    throw new InvalidOperationException(
                        $"Transaction {hash} not found. Consider restarting zcashd daemon using txindex=1 option.");
                }

                return tx;
            };

            var curr = await InternalGet(transactionHash);

            if (restoreInputs)
            {
                // TODO: use batch instead of subsequent queries

                foreach (var vin in curr.Vin)
                {
                    var prev = await InternalGet(vin.TxId);
                    var vout = prev.Vout.OrderBy(x => x.N).Skip((int)vin.Vout).First();

                    vin.Addresses = vout.ScriptPubKey.Addresses;
                    vin.Value = vout.Value;
                }
            }

            return curr;
        }

        public async Task<T> SendRpcAsync<T>(RPCOperations command, params object[] parameters)
        {
            var result = await _rpcClient.SendCommandAsync(command, parameters);

            result.ThrowIfError();

            // NBitcoin can not deserialize shielded tx data,
            // that's why custom models are used widely instead of built-in NBitcoin commands;
            // additionaly in case of exception we save context to investigate later:

            try
            {
                return result.Result.ToObject<T>();
            }
            catch (JsonSerializationException jex)
            {
                await _log.WriteErrorAsync(nameof(SendRpcAsync), $"Command: {command}, Response: {result.ResultString}", jex);
                throw;
            }
        }

        public async Task<RPCResponse> SendRpcAsync(RPCOperations command, params object[] parameters)
        {
            var result = await _rpcClient.SendCommandAsync(new RPCRequest(command, parameters), false);

            result.ThrowIfError();

            return result;
        }

        public Money CalcFee(TransactionBuilder builder, ISettings settings)
        {
            if (settings.UseDefaultFee)
            {
                return Constants.DefaultFee;
            }
            else
            {
                var fee = builder.EstimateFees(new FeeRate(Money.Coins(settings.FeePerKb)));
                var min = Money.Coins(settings.MinFee);
                var max = Money.Coins(settings.MaxFee);

                return Money.Max(Money.Min(fee, max), min);
            }
        }
    }
}
