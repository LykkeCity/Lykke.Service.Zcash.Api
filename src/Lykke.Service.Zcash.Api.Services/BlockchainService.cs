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

        private readonly Dictionary<string, AddressMonitorType> _transactionCategories = new Dictionary<string, AddressMonitorType>
        {
            ["receive"] = AddressMonitorType.To,
            ["send"] = AddressMonitorType.From
        };

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
            await RefreshSettingsAsync();

            var utxo = await SendRpcAsync<Utxo[]>(RPCOperations.listunspent, _settings.ConfirmationLevel, int.MaxValue, from);

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

                    var fee = CalculateFee(builder);
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
            await RefreshSettingsAsync();

            var summary = await SendRpcAsync<TransactionSummary>(RPCOperations.listsinceblock, 
                _settings.LastBlockHash, _settings.ConfirmationLevel, true);

            var lastTxs = from t in summary.Transactions
                          where t.Confirmations >= _settings.ConfirmationLevel && _transactionCategories.ContainsKey(t.Category)
                          group t by new { t.Category, t.Address, t.TxId, t.BlockTime } into g
                          select new
                          {
                              ToAddress = g.Key.Address,
                              Hash = g.Key.TxId,
                              Amount = Math.Abs(g.Sum(t => t.Amount)),
                              Timestamp = DateTimeOffset.FromUnixTimeSeconds(g.Key.BlockTime).UtcDateTime,
                              MonitorType = _transactionCategories[g.Key.Category],
                          };

            var sentTxs = (await _operationRepository.GetByStateAsync(TransactionState.Sent))
                .ToDictionary(tx => tx.Hash, t => t);

            foreach (var t in lastTxs)
            {
                if (sentTxs.TryGetValue(t.Hash, out var sent))
                {
                    await _operationRepository.UpdateAsync(sent.OperationId, 
                        completedUtc: t.Timestamp);
                }

                if (await IsObservableAsync(t.MonitorType, t.ToAddress))
                {
                    var fromAddress = await GetFromAddressAsync(t.Hash);
                    var operationId = await GetOperationIdAsync(t.Hash);

                    await _historyRepository.CreateAsync(operationId, t.Hash, t.Timestamp, 
                        fromAddress, t.ToAddress, t.Amount, Asset.Zec.Id);
                }
            }

            await SaveLastBlockAsync(summary.LastBlock);
        }

        public async Task<IEnumerable<ITransaction>> GetHistoryAsync(AddressMonitorType monitorType, string address, string afterHash = null, int take = 100)
        {
            return await _historyRepository.GetByAddressAsync(monitorType, address, afterHash, take);
        }

        public async Task<(string continuation, IEnumerable<AddressBalance> items)> GetBalancesAsync(string continuation = null, int take = 100)
        {
            var result = await _addressRepository.GetByTypeAsync(AddressMonitorType.Balance, continuation, take);

            var utxoParams = new List<object>();

            utxoParams.Add(_settings.ConfirmationLevel);
            utxoParams.Add(int.MaxValue);
            utxoParams.AddRange(result.items.Select(a => a.Address));

            var utxo = await SendRpcAsync<Utxo[]>(RPCOperations.listunspent, utxoParams.ToArray());

            var balances = utxo.GroupBy(u => u.Address)
                .Select(g => new AddressBalance
                {
                    Address = g.Key,
                    Balance = g.Sum(u => u.Amount),
                    AssetId = Asset.Zec.Id
                });

            return (result.continuation, balances);
        }

        public async Task<bool> TryCreateObservableAddressAsync(AddressMonitorType monitorType, string address)
        {
            await SendRpcAsync(RPCOperations.importaddress, address, null, false);

            return await _addressRepository.CreateIfNotExistsAsync(monitorType, address);
        }

        public async Task<bool> TryDeleteObservableAddressAsync(AddressMonitorType monitorType, string address)
        {
            return await _addressRepository.DeleteIfExistAsync(monitorType, address);
        }

        public async Task<bool> IsObservableAsync(AddressMonitorType monitorType, string address)
        {
            return (await _addressRepository.GetAsync(monitorType, address)) != null;
        }

        public Money CalculateFee(TransactionBuilder transactionBuilder)
        {
            if (_settings.UseDefaultFee)
            {
                return Constants.DefaultFee;
            }
            else
            {
                var fee = transactionBuilder.EstimateFees(new FeeRate(Money.Coins(_settings.FeePerKb)));
                var min = Money.Coins(_settings.MinFee);
                var max = Money.Coins(_settings.MaxFee);

                return Money.Max(Money.Min(fee, max), min);
            }
        }

        public async Task<string> GetFromAddressAsync(string transactionHash)
        {
            var result = new List<string>();
            var curr = await GetTransaction(transactionHash);

            foreach (var input in curr.Vin)
            {
                var prev = await GetTransaction(input.TxId);
                var vout = prev.Vout.OrderBy(x => x.N).Skip((int)input.Vout).First();

                result.AddRange(vout.ScriptPubKey.Addresses);
            }

            return result.FirstOrDefault();
        }

        public async Task<Guid> GetOperationIdAsync(string transactionHash)
        {
            var operationIndex = await _operationRepository.GetOperationIndexAsync(transactionHash);

            if (operationIndex != null)
                return operationIndex.OperationId;
            else
                return Guid.Empty;
        }

        public async Task RefreshSettingsAsync()
        {
            var settings = await _settingsRepository.GetAsync();

            if (settings != null)
            {
                _settings.ConfirmationLevel = settings.ConfirmationLevel ?? _settings.ConfirmationLevel;
                _settings.LastBlockHash = settings.LastBlockHash ?? _settings.LastBlockHash; 
                _settings.FeePerKb = settings.FeePerKb ?? _settings.FeePerKb;
                _settings.MaxFee = settings.MaxFee ?? _settings.MaxFee;
                _settings.MinFee = settings.MinFee ?? _settings.MinFee;
            }
        }

        public async Task SaveLastBlockAsync(string blockHash)
        {
            await _settingsRepository.UpsertAsync(lastBlockHash: blockHash);
        }

        public async Task<RawTransaction> GetTransaction(string transactionHash)
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
    }
}
