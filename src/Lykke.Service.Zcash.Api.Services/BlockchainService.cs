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

        public async Task<IOperation> BuildAsync(Guid operationId, BitcoinAddress from, BitcoinAddress to, Money amount, Asset asset, bool subtractFees)
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

        public async Task BroadcastAsync(IOperation operation, string transaction)
        {
            try
            {
                var transactionHash = await SendRpcAsync<string>(RPCOperations.sendrawtransaction, transaction);

                await _operationRepository.UpdateAsync(operation.OperationId, signedTransaction: transaction,
                    sentUtc: DateTime.UtcNow, hash: transactionHash);
            }
            catch (RPCException ex)
            {
                await _operationRepository.UpdateAsync(operation.OperationId, signedTransaction: transaction,
                    failedUtc: DateTime.UtcNow, error: ex.ToString());
            }
        }

        public async Task<IOperation> GetOperationAsync(Guid operationId)
        {
            return await _operationRepository.GetAsync(operationId);
        }

        public async Task<bool> TryDeleteOperationAsync(Guid operationId)
        {
            var operation = await _operationRepository.GetAsync(operationId);
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
                    }
                }
            }

            await SaveStoredSettingsAsync(recent.LastBlock);
        }

        public async Task<IEnumerable<IHistoryItem>> GetHistoryAsync(ObservationCategory category, string address, string afterHash = null, int take = 100)
        {
            return await _historyRepository.GetByAddressAsync(category, address, afterHash, take);
        }

        public async Task<(string continuation, IEnumerable<AddressBalance> items)> GetBalancesAsync(string continuation = null, int take = 100)
        {
            var settings = await LoadStoredSettingsAsync();

            var addressQuery = await _addressRepository.GetBySubjectAsync(ObservationCategory.Balance, continuation, take);

            var utxoParams = new List<object>();

            utxoParams.Add(settings.ConfirmationLevel);
            utxoParams.Add(int.MaxValue);
            utxoParams.AddRange(addressQuery.items.Select(x => x.Address));

            var utxo = await SendRpcAsync<Utxo[]>(RPCOperations.listunspent, utxoParams.ToArray());

            var balances = utxo.GroupBy(x => x.Address)
                .Select(g => new AddressBalance
                {
                    Address = g.Key,
                    Balance = g.Sum(x => x.Amount),
                    AssetId = Asset.Zec.Id
                })
                .ToList();

            return (addressQuery.continuation, balances);
        }

        public async Task<bool> TryCreateObservableAddressAsync(ObservationCategory category, string address)
        {
            var observableAddress = await _addressRepository.GetAsync(category, address);

            if (observableAddress == null)
            {
                await SendRpcAsync(RPCOperations.importaddress, address, string.Empty, false);
                await _addressRepository.Create(category, address);
                return true;
            }

            return false;
        }

        public async Task<bool> TryDeleteObservableAddressAsync(ObservationCategory category, string address)
        {
            var observableAddress = await _addressRepository.GetAsync(category, address);

            if (observableAddress != null)
            {
                await _addressRepository.Delete(category, address);
                return true;
            }

            return false;
        }

        public async Task<bool> IsObservableAsync(ObservationCategory category, string address)
        {
            return (await _addressRepository.GetAsync(category, address)) != null;
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
            async Task<RawTransaction> InternalGet(string hash)
            {
                var tx = await SendRpcAsync<RawTransaction>(RPCOperations.getrawtransaction, hash, 1);
                if (tx == null)
                {
                    throw new InvalidOperationException(
                        $"Transaction {hash} not found. Consider restarting zcashd daemon with txindex=1 option.");
                }

                return tx;
            };

            var curr = await InternalGet(transactionHash);

            foreach (var vin in curr.Vin)
            {
                var prev = await InternalGet(vin.TxId);
                var vout = prev.Vout.OrderBy(x => x.N).Skip((int)vin.Vout).First();

                vin.Addresses = vout.ScriptPubKey.Addresses;
                vin.Value = vout.Value;
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
