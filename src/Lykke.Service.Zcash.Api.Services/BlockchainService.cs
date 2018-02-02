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
using NBitcoin.Policy;

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

        public bool ValidateAddress(string address)
        {
            return Core.Utils.ValidateAddress(address, out var _);
        }

        public void EnsureSigned(Transaction transaction, ICoin[] coins)
        {
            // check transaction sign only (due to differences between BTC and ZEC fee calculation)
            if (!new TransactionBuilder()
                .AddCoins(coins)
                .SetTransactionPolicy(new StandardTransactionPolicy { CheckFee = false })
                .Verify(transaction, out var errors))
            {
                throw new InvalidOperationException(errors.ToStringViaSeparator(Environment.NewLine));
            }
        }

        public async Task<string> BuildAsync(Guid operationId, OperationType type, Asset asset, bool subtractFee, (BitcoinAddress from, BitcoinAddress to, Money amount)[] items)
        {
            var settings = await LoadStoredSettingsAsync();

            var inputs = 
                items.GroupBy(x => x.from)
                     .Select(g => new { Address = g.Key, Amount = g.Select(x => x.amount).Sum() })
                     .ToList();

            var outputs = 
                items.GroupBy(x => x.to)
                     .Select(g => new { Address = g.Key, Amount = g.Select(x => x.amount).Sum() })
                     .ToList();

            var utxo = await _blockchainReader.ListUnspentAsync(
                settings.ConfirmationLevel,
                inputs.Select(from => from.Address.ToString()).ToArray());

            var unspentOutputs = 
                inputs.ToDictionary(from => from.Address, from => new Stack<Utxo>(utxo.Where(x => x.Address == from.Address.ToString()).OrderBy(x => x.Confirmations)));

            var spentOutputs = 
                inputs.ToDictionary(from => from.Address, from => new Stack<Utxo>());

            var oddOutputs = 
                inputs.ToDictionary(from => from.Address, from => (TxOut)null);

            var tx = new Transaction();

            foreach (var from in inputs)
            {
                var amount = from.Amount;

                if (amount > Money.Zero)
                {
                    while (amount > Money.Zero && unspentOutputs[from.Address].TryPop(out var vout))
                    {
                        tx.AddInput(vout.AsTxIn());
                        spentOutputs[from.Address].Push(vout);
                        amount -= vout.Money;
                    }
                }

                if (amount > Money.Zero)
                {
                    throw new NotEnoughFundsException("Not enough funds", from.Address.ToString(), amount);
                }

                if (amount < Money.Zero)
                {
                    oddOutputs[from.Address] = tx.AddOutput(amount.Abs(), from.Address);
                }
            }

            foreach (var to in outputs)
            {
                tx.AddOutput(to.Amount, to.Address);
            }

            var fee = CalcFee(tx, settings);
            var totalAmount = items.Select(x => x.amount).Sum();

            if (subtractFee)
            {
                foreach (var vout in tx.Outputs.Except(oddOutputs.Where(x => x.Value != null).Select(x => x.Value)))
                {
                    vout.Value -= CalcFeeSplit(fee, totalAmount, vout.Value);
                }
            }
            else
            {
                foreach (var from in inputs)
                {
                    var inputAmount = spentOutputs[from.Address].Select(x => x.Money).Sum();
                    var operationAndFeeAmount = from.Amount + CalcFeeSplit(fee, totalAmount, from.Amount);

                    if (inputAmount < operationAndFeeAmount)
                    {
                        while (inputAmount < operationAndFeeAmount && unspentOutputs[from.Address].TryPop(out var vout))
                        {
                            tx.AddInput(vout.AsTxIn());
                            spentOutputs[from.Address].Push(vout);
                            inputAmount += vout.Money;
                        }
                    }

                    if (inputAmount < operationAndFeeAmount)
                    {
                        throw new NotEnoughFundsException("Not enough funds", from.ToString(), operationAndFeeAmount - inputAmount);
                    }

                    if (inputAmount > operationAndFeeAmount)
                    {
                        oddOutputs[from.Address] = oddOutputs[from.Address] ?? tx.AddOutput(0, from.Address);
                        oddOutputs[from.Address].Value = inputAmount - operationAndFeeAmount;
                    }
                    else if (oddOutputs.TryGetValue(from.Address, out var vout)) // must always be true here
                    {
                        tx.Outputs.Remove(vout);
                        oddOutputs[from.Address] = null;
                    }
                }
            }

            await _operationRepository.UpsertAsync(operationId, type,
                items.Select(x => (x.from.ToString(), x.to.ToString(), x.amount.ToUnit(asset.Unit))).ToArray(),
                fee.ToUnit(asset.Unit), subtractFee, asset.Id);

            var coins = spentOutputs.Values
                .SelectMany(v => v)
                .Select(x => x.AsCoin())
                .ToList();

            return Serializer.ToString((tx, coins));
        }

        public async Task BroadcastAsync(Guid operationId, Transaction transaction)
        {
            var hash = await _blockchainReader.SendRawTransactionAsync(transaction);

            await _operationRepository.UpdateAsync(operationId, sentUtc: DateTime.UtcNow, hash: hash);
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
            if (await IsObservableAsync(category, address))
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
            var addressQuery = await _addressRepository.GetByCategoryAsync(ObservationCategory.Balance, continuation, take);

            if (addressQuery.items.Any())
            {
                var utxo = await _blockchainReader.ListUnspentAsync(settings.ConfirmationLevel, addressQuery.items.Select(x => x.Address).ToArray());

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
            var addressInfo = await _blockchainReader.ValidateAddressAsync(address);

            if (!addressInfo.IsValid)
            {
                throw new InvalidOperationException($"Invalid Zcash address: {address}");
            }

            if (!addressInfo.IsMine && !addressInfo.IsWatchOnly)
            {
                await _blockchainReader.ImportAddressAsync(address);
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

            return curr;
        }

        public Money CalcFee(Transaction tx, ISettings settings)
        {
            if (settings.UseDefaultFee)
            {
                return Constants.DefaultFee;
            }
            else
            {
                var fee = new FeeRate(Money.Coins(settings.FeePerKb)).GetFee(tx);
                var min = Money.Coins(settings.MinFee);
                var max = Money.Coins(settings.MaxFee);

                return Money.Max(Money.Min(fee, max), min);
            }
        }

        public Money CalcFeeSplit(Money fee, Money totalOutput, Money output)
        {
            var unit = Asset.Zec.Unit;
            var decimalFee = fee.ToUnit(unit);
            var decimalTotalOutput = totalOutput.ToUnit(unit);
            var decimalOutput = output.ToUnit(unit);
            var decimalResult = decimalFee * (decimalOutput / decimalTotalOutput);

            return Money.FromUnit(decimalResult, unit);
        }
    }
}
