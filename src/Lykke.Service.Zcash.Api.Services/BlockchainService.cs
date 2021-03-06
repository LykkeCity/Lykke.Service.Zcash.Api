﻿using System.Globalization;
using System;
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
using MoreLinq;
using Newtonsoft.Json;

namespace Lykke.Service.Zcash.Api.Services
{
    public class BlockchainService : IBlockchainService
    {
        private readonly ILog _log;
        private readonly IBlockchainReader _blockchainReader;
        private readonly IAddressRepository _addressRepository;
        private readonly IOperationRepository _operationRepository;
        private readonly IHistoryRepository _historyRepository;
        private readonly ISettingsRepository _settingsRepository;
        private readonly ZcashApiSettings _settings;

        public BlockchainService(
            ILog log,
            IBlockchainReader blockchainReader,
            IAddressRepository addressRepository,
            IOperationRepository operationRepository,
            IHistoryRepository historyRepository,
            ISettingsRepository settingsRepository,
            ZcashApiSettings settings)
        {
            _log = log;
            _blockchainReader = blockchainReader;
            _addressRepository = addressRepository;
            _operationRepository = operationRepository;
            _historyRepository = historyRepository;
            _settingsRepository = settingsRepository;
            _settings = settings;
        }

        public async Task<(string context, Dictionary<string, decimal> outputs)> BuildAsync(
            Guid operationId, OperationType type, Asset asset, bool subtractFees, params (string from, string to, decimal amount)[] items)
        {
            var settings = await LoadStoredSettingsAsync();

            var info = await _blockchainReader.GetInfoAsync();

            var fromAddresses = 
                items.GroupBy(x => x.from)
                     .ToDictionary(g => g.Key, g => g.Select(x => x.amount).Sum());

            var toAddresses = 
                items.GroupBy(x => x.to)
                     .ToDictionary(g => g.Key, g => g.Select(x => x.amount).Sum());

            var utxo = await _blockchainReader.ListUnspentAsync(settings.ConfirmationLevel, fromAddresses.Keys.ToArray());

            var unspentUtxo = 
                fromAddresses.Keys.ToDictionary(from => from, from => new Stack<Utxo>(utxo.Where(x => x.Address == from).OrderBy(x => x.Confirmations)));

            var spentUtxo = 
                fromAddresses.Keys.ToDictionary(from => from, from => new Stack<Utxo>());

            var change = 
                fromAddresses.Keys.ToDictionary(from => from, from => 0m);

            foreach (var from in fromAddresses.Keys)
            {
                var amount = fromAddresses[from];

                if (amount > 0m)
                {
                    while (amount > 0m && unspentUtxo[from].TryPop(out var vout))
                    {
                        spentUtxo[from].Push(vout);
                        amount -= vout.Amount;
                    }
                }

                if (amount > 0m)
                {
                    throw new BlockchainException(BlockchainException.ErrorCode.NotEnoughFunds, $"Not enough funds: {{{from}:{amount}}}.");
                }

                if (amount < 0m)
                {
                    change[from] = Math.Abs(amount);
                }
            }

            var fee = CalcFee(fromAddresses.Count, toAddresses.Count, settings);
            var totalAmount = items.Select(x => x.amount).Sum();

            if (subtractFees)
            {
                foreach (var to in toAddresses.Keys.ToList())
                {
                    // we'll check for dust later
                    toAddresses[to] -= CalcFeeSplit(fee, totalAmount, toAddresses[to]);
                }
            }
            else
            {
                foreach (var from in fromAddresses.Keys)
                {
                    var spentAmount = spentUtxo[from].Sum(x => x.Amount);
                    var operationAndFeeAmount = fromAddresses[from] + CalcFeeSplit(fee, totalAmount, fromAddresses[from]);

                    if (spentAmount < operationAndFeeAmount)
                    {
                        while (spentAmount < operationAndFeeAmount && unspentUtxo[from].TryPop(out var vout))
                        {
                            spentUtxo[from].Push(vout);
                            spentAmount += vout.Amount;
                        }
                    }

                    if (spentAmount < operationAndFeeAmount)
                    {
                        throw new BlockchainException(BlockchainException.ErrorCode.NotEnoughFunds, $"Not enough funds: {{{from}:{operationAndFeeAmount - spentAmount}}}.");
                    }

                    if (spentAmount > operationAndFeeAmount)
                    {
                        change[from] = spentAmount - operationAndFeeAmount;
                    }
                }
            }

            foreach (var to in toAddresses.Keys)
            {
                // output is considered as dust if it's less than three
                // times greater than fee required to relay this output
                if (toAddresses[to] <= 0.182m * 3 * info.RelayFee)
                {
                    throw new BlockchainException(BlockchainException.ErrorCode.Dust, $"Dust output: {{{to}:{toAddresses[to]}}}.");
                }
            }

            var inputs = spentUtxo
                .SelectMany(x => x.Value)
                .ToArray();

            var outputs = toAddresses
                .FullJoin(change, x => x.Key, a => a, b => b, (a, b) => new KeyValuePair<string, decimal>(a.Key, a.Value + b.Value))
                .Where(x => x.Value > 0m)
                .ToDictionary();

            var hex = await _blockchainReader.CreateRawTransaction(inputs, outputs);

            var raw = await _blockchainReader.DecodeRawTransaction(hex);

            var blockchainInfo = await _blockchainReader.GetBlockchainInfo();

            var branchId = uint.Parse(blockchainInfo.Consensus.NextBlock, NumberStyles.HexNumber);

            var ctx = JsonConvert.SerializeObject((hex, inputs, branchId));

            await _operationRepository.UpsertAsync(operationId, type, items, fee, subtractFees, asset.Id, raw.ExpiryHeight);

            return (ctx, outputs);
        }

        public async Task BroadcastAsync(Guid operationId, string transaction)
        {
            try
            {
                var hash = await _blockchainReader.SendRawTransactionAsync(transaction);

                await _operationRepository.UpdateAsync(operationId, 
                    sentUtc: DateTime.UtcNow, hash: hash);
            }
            catch (NBitcoin.RPC.RPCException ex) when (ex.RPCCode == NBitcoin.RPC.RPCErrorCode.RPC_VERIFY_REJECTED)
            {
                await _log.WriteWarningAsync(nameof(BroadcastAsync),
                    $"Transaction: {transaction}, Error: {ex.ToString()}",
                    $"Transaction rejected");

                throw new BlockchainException(BlockchainException.ErrorCode.Rejected, "Transaction rejected");
            }
        }

        public async Task<IOperation> GetOperationAsync(Guid operationId, bool loadItems = true)
        {
            return await _operationRepository.GetAsync(operationId, loadItems);
        }

        public async Task<bool> TryDeleteOperationAsync(Guid operationId)
        {
            var operation = await _operationRepository.GetAsync(operationId, false);

            if (operation != null && 
                operation.State != OperationState.Deleted)
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

            var recent = await _blockchainReader.ListSinceBlockAsync(settings.LastBlockHash, settings.ConfirmationLevel);

            var recentTransactions = from t in recent.Transactions
                                     where t.Category == receiveCategory || t.Category == sendCategory
                                     where t.Confirmations >= settings.ConfirmationLevel
                                     group t by t.TxId into g
                                     select g.Key;

            var recorded = 0;

            foreach (var hash in recentTransactions)
            {
                var transactionActions = new RawTransactionAction[0];
                var raw = await GetRawTransactionAsync(hash);
                var timestamp = raw.BlockTime.FromUnixDateTime();
                var operation = await _operationRepository.GetAsync(hash);

                if (operation != null)
                {
                    await _operationRepository.UpdateAsync(operation.OperationId, minedUtc: timestamp, completedUtc: DateTime.UtcNow, block: raw.Block.Height);

                    transactionActions = operation.GetRawTransactionActions();
                }
                else
                {
                    transactionActions = raw.GetActions();
                }

                foreach (var action in transactionActions)
                {
                    if (await _addressRepository.IsHistoryAddressExistsAsync(action.AffectedAddress, action.Category))
                    {
                        await _historyRepository.UpsertAsync(action.Category, action.AffectedAddress, timestamp, hash,
                            operation?.OperationId, action.FromAddress, action.ToAddress, action.Amount, action.AssetId);

                        recorded++;
                    }
                }
            }

            // we've processed confirmed transactions only,
            // so to prevent marking most likely valid, but not yet processed transactions as failed
            // we must use last confirmed block instead of the best block height to check expiration

            var lastBlock = await _blockchainReader.GetBlockAsync(recent.LastBlock);

            await _operationRepository.UpdateExpiredAsync(lastBlock.Height);

            await SaveStoredSettingsAsync(recent.LastBlock);

            await _log.WriteInfoAsync(nameof(HandleHistoryAsync), 
                $"Range: [{settings.LastBlockHash} - {recent.LastBlock}]",
                $"History handled. {recorded} of {recent.Transactions.Length} recorded");
        }

        public async Task<IEnumerable<IHistoryItem>> GetHistoryAsync(HistoryAddressCategory category, string address, string afterHash = null, int take = 100)
        {
            if (await _addressRepository.IsHistoryAddressExistsAsync(address, category))
            {
                return await _historyRepository.GetByAddressAsync(category, address, afterHash, take);
            }
            else
            {
                return new IHistoryItem[0];
            }
        }

        public async Task<(string continuation, IEnumerable<AddressBalance> items)> GetBalancesAsync(string continuation = null, int take = 100)
        {
            var settings = await LoadStoredSettingsAsync();
            var balances = new List<AddressBalance>();
            var info = await _blockchainReader.GetInfoAsync();
            var confirmedBlock = info.Blocks - (uint)settings.ConfirmationLevel + 1;

            do
            {
                var addressQuery = await _addressRepository.GetBalanceAddressesChunkAsync(continuation, take);

                if (addressQuery.items.Any())
                {
                    var utxo = await _blockchainReader.ListUnspentAsync(settings.ConfirmationLevel, addressQuery.items.ToArray());

                    foreach (var group in utxo.GroupBy(x => x.Address))
                    {
                        balances.Add(new AddressBalance
                        {
                            Address = group.Key,
                            Balance = group.Sum(x => x.Amount),
                            Asset = Asset.Zec,
                            Block = confirmedBlock
                        });
                    }
                }

                take -= balances.Count;
                continuation = addressQuery.continuation;
            }
            while (take > 0 && !string.IsNullOrEmpty(continuation));

            return (continuation, balances);
        }

        public async Task<(string continuation, IEnumerable<string> items)> GetObservableAddressesAsync(AddressType type, string continuation = null, int take = 100)
        {
            switch (type)
            {
                case AddressType.Balance:
                    return await _addressRepository.GetBalanceAddressesChunkAsync(continuation, take);

                case AddressType.History:
                    return await _addressRepository.GetHistoryAddressesChunkAsync(continuation, take);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public async Task<bool> TryCreateBalanceAddressAsync(string address)
        {
            await ImportAddress(address);

            return await _addressRepository.CreateBalanceAddressIfNotExistsAsync(address);
        }

        public async Task<bool> TryDeleteBalanceAddressAsync(string address)
        {
            return await _addressRepository.DeleteBalanceAddressIfExistsAsync(address);
        }

        public async Task<bool> TryCreateHistoryAddressAsync(string address, HistoryAddressCategory category)
        {
            await ImportAddress(address);

            return await _addressRepository.CreateHistoryAddressIfNotExistsAsync(address, category);
        }

        public async Task<bool> TryDeleteHistoryAddressAsync(string address, HistoryAddressCategory category)
        {
            return await _addressRepository.DeleteHistoryAddressIfExistsAsync(address, category);
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

        public async Task ImportAddress(string address)
        {
            var addressInfo = await _blockchainReader.ValidateAddressAsync(address);

            if (!addressInfo.IsValid)
            {
                throw new InvalidOperationException($"Invalid Zcash address: {address}");
            }

            if (!addressInfo.IsMine && !addressInfo.IsWatchOnly)
            {
                await _blockchainReader.ImportAddressAsync(address);
            }
        }

        public async Task<RawTransaction> GetRawTransactionAsync(string transactionHash, bool restoreInputs = true, bool restoreBlock = true)
        {
            async Task<RawTransaction> InternalGet(string hash)
            {
                var tx = await _blockchainReader.GetRawTransactionAsync(hash);
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

            if (restoreBlock)
            {
                curr.Block = await _blockchainReader.GetBlockAsync(curr.BlockHash);
            }

            return curr;
        }

        public async Task<bool> ValidateAddressAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            return (await _blockchainReader.ValidateAddressAsync(address)).IsValid;
        }

        public async Task<bool> ValidateSignedTransactionAsync(string transaction)
        {
            if (string.IsNullOrEmpty(transaction))
            {
                return false;
            }

            try
            {
                var tx = await _blockchainReader.DecodeRawTransaction(transaction);

                return tx != null &&
                    tx.Vin.All(vin => vin.ScriptSig != null && !string.IsNullOrEmpty(vin.ScriptSig.Asm) && !string.IsNullOrEmpty(vin.ScriptSig.Hex));
            }
            catch (Exception ex)
            {
                await _log.WriteWarningAsync(nameof(ValidateSignedTransactionAsync),
                    $"Transaction: {transaction}, Error: {ex.ToString()}",
                    $"Error while decoding transaction");

                return false;
            }
        }

        public decimal CalcFee(int numInputs, int numOutputs, ISettings settings)
        {
            if (settings.UseDefaultFee)
            {
                return Constants.DefaultFee;
            }
            else
            {
                // for Zcash the formula is:
                // 15 + (numInputs × 148) + (numOutputs × 34) + (numJoinSplits × 1802) + (numJoinSplits > 0 ? 96 : 0)
                // we don't use z-transactions so we don't have joinSplits:

                var fee = settings.FeePerKb * (15 + (numInputs * 148) + (numOutputs * 34)) / 1024;
                var min = settings.MinFee;
                var max = settings.MaxFee;

                return Math.Round(Math.Max(Math.Min(fee, max), min), Asset.Zec.DecimalPlaces);
            }
        }

        public decimal CalcFeeSplit(decimal fee, decimal totalOutput, decimal output)
        {
            return fee * (output / totalOutput);
        }

        public string[] GetExplorerUrl(string address)
        {
            if (NBitcoin.Network.GetNetwork(_settings.NetworkType) == NBitcoin.Zcash.ZcashNetworks.Instance.Mainnet)
            {
                return 
                    _settings.MainNetExplorerAddressUrls?.Select(url => string.Format(url, address))?.ToArray() ??
                    new string[]
                    {
                        $"https://explorer.zcha.in/accounts/{address}"
                    };
            }

            if (NBitcoin.Network.GetNetwork(_settings.NetworkType) == NBitcoin.Zcash.ZcashNetworks.Instance.Testnet)
            {
                return
                    _settings.TestNetExplorerAddressUrls?.Select(url => string.Format(url, address))?.ToArray() ??
                    new string[]
                    {
                        $"https://explorer.testnet.z.cash/address/{address}"
                    };
            }

            return new string[0];
        }
    }
}
