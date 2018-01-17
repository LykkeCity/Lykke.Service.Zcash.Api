using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using NBitcoin;
using NBitcoin.JsonConverters;
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

        static BlockchainService()
        {
            ZcashNetworks.Register();
        }

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

        public async Task<IOperationalTransaction> BuildNotSignedTxAsync(Guid operationId, BitcoinAddress from, BitcoinAddress to, Money amount, Asset asset, bool subtractFees)
        {
            var settings = await LoadStoredSettingsAsync();

            var utxo = await SendRpcAsync<Utxo[]>(RPCOperations.listunspent, settings.ConfirmationLevel, int.MaxValue, from);

            utxo = utxo
                .OrderByDescending(uc => uc.Confirmations)
                .ThenBy(uc => uc.TxId)
                .ThenBy(uc => uc.Vout)
                .ToArray();

            if (utxo.Any())
            {
                var totalIn = Money.Zero;
                var builder = new TransactionBuilder().Send(to, amount).SetChange(from);

                if (subtractFees)
                {
                    builder.SubtractFees();
                }

                foreach (var item in utxo)
                {
                    builder.AddCoins(item.AsCoin());

                    totalIn += item.Money;

                    var fee = CalcFee(builder, settings);
                    var totalOut = subtractFees
                        ? amount - fee
                        : amount + fee;

                    if (totalIn >= totalOut)
                    {
                        var tx = builder.SendFees(fee).BuildTransaction(sign: false);
                        var signContext = Serializer.ToString((tx: tx, coins: builder.FindSpentCoins(tx)));

                        return await _operationRepository.CreateAsync(operationId, from.ToString(),
                            to.ToString(), asset.Id, amount.ToUnit(asset.Unit), fee.ToUnit(asset.Unit), signContext);
                    }
                }
            }

            throw new InvalidOperationException("Insufficient funds");
        }

        public async Task BroadcastTxAsync(IOperationalTransaction tx, string transaction)
        {
            try
            {
                var transactionHash = await SendRpcAsync<string>(RPCOperations.sendrawtransaction, transaction);

                await _operationRepository.UpdateAsync(tx.OperationId, signedTransaction: transaction,
                    sentUtc: DateTime.UtcNow, hash: transactionHash);
            }
            catch (RPCException ex)
            {
                await _operationRepository.UpdateAsync(tx.OperationId, signedTransaction: transaction,
                    failedUtc: DateTime.UtcNow, error: ex.ToString());
            }
        }

        public async Task<IOperationalTransaction> GetOperationalTxAsync(Guid operationId)
        {
            return await _operationRepository.GetAsync(operationId);
        }

        public async Task<bool> TryDeleteOperationalTxAsync(Guid operationId)
        {
            return await _operationRepository.DeleteIfExistAsync(operationId);
        }

        public async Task HandleHistoryAsync()
        {
            var settings = await LoadStoredSettingsAsync();

            var recent = await SendRpcAsync<RecentTransactionSummary>(RPCOperations.listsinceblock,
                settings.LastBlockHash, settings.ConfirmationLevel, 1);

            var sent = (await _operationRepository.GetByStateAsync(OperationState.Sent))
                .ToDictionary(tx => tx.Hash, tx => tx);

            foreach (var transaction in recent.GetConfirmed(settings.ConfirmationLevel))
            {
                // update operation state

                if (sent.TryGetValue(transaction.Hash, out var tx))
                {
                    await _operationRepository.UpdateAsync(tx.OperationId, completedUtc: transaction.TimestampUtc);
                }

                // save history for observable address

                var operationId = tx?.OperationId 
                    ?? await _operationRepository.GetOperationIdAsync(transaction.Hash) 
                    ?? Guid.Empty;

                var from = await GetSendersAsync(transaction.Hash);

                // default values for "to" history
                var fromAddress = from.FirstOrDefault();
                var observables = new[] 
                {
                    transaction.ToAddress
                };

                // override defaults for "from" history
                if (transaction.ObservationSubject == ObservationSubject.From)
                {
                    fromAddress = null; // will use observable address instead
                    observables = from;
                }

                foreach (var addr in observables)
                {
                    if (await IsObservableAsync(transaction.ObservationSubject, addr))
                    {
                        await _historyRepository.UpsertAsync(transaction.ObservationSubject, operationId,
                            transaction.Hash, transaction.TimestampUtc, fromAddress ?? addr, transaction.ToAddress, transaction.Amount, transaction.AssetId);
                    }
                }
            }

            await SaveStoredSettingsAsync(recent.LastBlock);
        }

        public async Task<IEnumerable<ITransaction>> GetHistoryAsync(ObservationSubject subject, string address, string afterHash = null, int take = 100)
        {
            return await _historyRepository.GetByAddressAsync(subject, address, afterHash, take);
        }

        public async Task<(string continuation, IEnumerable<AddressBalance> items)> GetBalancesAsync(string continuation = null, int take = 100)
        {
            var settings = await LoadStoredSettingsAsync();

            var result = await _addressRepository.GetBySubjectAsync(ObservationSubject.Balance, continuation, take);

            var utxoParams = new List<object>();

            utxoParams.Add(settings.ConfirmationLevel);
            utxoParams.Add(int.MaxValue);
            utxoParams.AddRange(result.items.Select(a => a.Address));

            var utxo = await SendRpcAsync<Utxo[]>(RPCOperations.listunspent, utxoParams.ToArray());

            var balances = utxo.GroupBy(u => u.Address)
                .Select(g => new AddressBalance
                {
                    Address = g.Key,
                    Balance = g.Sum(u => u.Amount),
                    AssetId = Asset.Zec.Id
                })
                .ToList();

            return (result.continuation, balances);
        }

        public async Task<bool> TryCreateObservableAddressAsync(ObservationSubject subject, string address)
        {
            await SendRpcAsync(RPCOperations.importaddress, address, null, false);

            return await _addressRepository.CreateIfNotExistsAsync(subject, address);
        }

        public async Task<bool> TryDeleteObservableAddressAsync(ObservationSubject subject, string address)
        {
            return await _addressRepository.DeleteIfExistAsync(subject, address);
        }

        public async Task<bool> IsObservableAsync(ObservationSubject subject, string address)
        {
            return (await _addressRepository.GetAsync(subject, address)) != null;
        }

        public async Task<string[]> GetSendersAsync(string transactionHash)
        {
            var result = new List<string>();
            var curr = await GetRawTransactionAsync(transactionHash);

            foreach (var vin in curr.Vin)
            {
                var prev = await GetRawTransactionAsync(vin.TxId);
                var vout = prev.Vout.OrderBy(x => x.N).Skip((int)vin.Vout).First();

                result.AddRange(vout.ScriptPubKey.Addresses);
            }

            return result.Distinct().ToArray();
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

        public async Task<RawTransaction> GetRawTransactionAsync(string transactionHash)
        {
            var tx = await SendRpcAsync<RawTransaction>(RPCOperations.getrawtransaction, transactionHash, 1);
            if (tx == null)
            {
                throw new InvalidOperationException(
                    $"Transaction {transactionHash} not found. Consider restarting zcashd daemon with txindex=1 option.");
            }

            return tx;
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
            var result = await _rpcClient.SendCommandAsync(command, parameters);

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
