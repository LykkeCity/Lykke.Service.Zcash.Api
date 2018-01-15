using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;
using Lykke.Service.Zcash.Api.Core.Repositories;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Zcash.Api.Services.Models;
using NBitcoin;
using NBitcoin.JsonConverters;
using NBitcoin.RPC;
using NBitcoin.Zcash;

namespace Lykke.Service.Zcash.Api.Services
{
    public class BlockchainService : IBlockchainService
    {
        private const string TX_CATEGORY_RECEIVE = "receive";
        private const string TX_CATEGORY_SEND = "send";

        private readonly RPCClient _rpcClient;
        private readonly IAddressRepository _addressRepository;
        private readonly IOperationRepository _operationRepository;
        private readonly IHistoryRepository _historyRepository;
        private readonly ZcashApiSettings _settings;
        private readonly FeeRate _feeRate;

        static BlockchainService()
        {
            ZcashNetworks.Register();
        }

        public BlockchainService(
            RPCClient rpcClient, 
            IAddressRepository addressRepository, 
            IOperationRepository operationRepository,
            IHistoryRepository historyRepository,
            ZcashApiSettings settings)
        {
            _rpcClient = rpcClient;
            _addressRepository = addressRepository;
            _operationRepository = operationRepository;
            _historyRepository = historyRepository;
            _settings = settings;
            _feeRate = new FeeRate(Money.Coins(_settings.FeePerKb));
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
            var utxo = await _rpcClient.ListUnspentAsync(_settings.ConfirmationLevel, int.MaxValue, from);

            utxo = utxo.OrderByDescending(uc => uc.Confirmations)
                .ThenBy(uc => uc.OutPoint.Hash)
                .ThenBy(uc => uc.OutPoint.N)
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

                    totalIn += item.Amount;

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

        public async Task BroadcastTxAsync(IOperationalTransaction tx, Transaction transaction)
        {
            try
            {
                await _rpcClient.SendRawTransactionAsync(transaction);

                await _operationRepository.UpdateAsync(tx.OperationId, signedTransaction: transaction.ToHex(),
                    sentUtc: DateTime.Now.ToUniversalTime(), 
                    hash: transaction.GetHash().ToString());
            }
            catch (Exception ex)
            {
                await _operationRepository.UpdateAsync(tx.OperationId, signedTransaction: transaction.ToHex(),
                    failedUtc: DateTime.Now.ToUniversalTime(), 
                    error: ex.ToString());
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
            var lastProcessedBlock = _blocksRepository.GetLastProcessedBlockHash();

            var command = await _rpcClient.SendCommandAsync(RPCOperations.listsinceblock, 
                lastProcessedBlock, _settings.ConfirmationLevel, true);

            command.ThrowIfError();

            var lastTxsResult = command.Result.ToObject<LastTransactionResult>();

            var lastTxs = from t in lastTxsResult.Transactions
                          where t.Confirmations >= _settings.ConfirmationLevel
                          where t.Category == TX_CATEGORY_RECEIVE || t.Category == TX_CATEGORY_SEND
                          group t by new { t.Category, t.Address, t.TxId, t.BlockTime } into g
                          select new
                          {
                              g.Key.Category,
                              ToAddress = g.Key.Address,
                              Hash = g.Key.TxId,
                              Amount = Math.Abs(g.Sum(t => t.Amount)),
                              Fee = Math.Abs(g.Sum(t => t.Fee) ?? 0M),
                              Timestamp = DateTimeOffset.FromUnixTimeSeconds(g.Key.BlockTime).UtcDateTime
                          };

            var sentTxs = (await _operationRepository.GetByStateAsync(TransactionState.Sent))
                .ToDictionary(tx => tx.Hash, t => t);

            foreach (var t in lastTxs)
            {
                if (sentTxs.TryGetValue(t.Hash, out var sent))
                {
                    await _operationRepository.UpdateAsync(sent.OperationId, completedUtc: t.Timestamp);
                }

                if (await IsObservableAsync(t.Category, t.ToAddress))
                {
                    var fromAddresses = await GetFromAddresses(t.Hash, t.ToAddress);
                    var from = fromAddresses.FirstOrDefault();
                    var operationId = sent?.OperationId ?? Guid.Empty;
                    var assetId = Asset.Zec.Id;

                    await _historyRepository.CreateAsync(operationId,
                        t.Hash, t.Timestamp, from, t.ToAddress, t.Amount, t.Fee, assetId);
                }
            }

            _blocksRepository.SaveLastProcessedBlockHash(lastTxs.LastBlock);
        }

        public async Task<ITransaction[]> GetHistoryAsync(AddressMonitorType type, string address, string afterHash = null, int take = 100)
        {
            return await _historyRepository.GetAsync(type, address, afterHash, take);
        }

        public async Task<(string continuation, AddressBalance[] items)> GetBalancesAsync(string continuation = null, int take = 100)
        {
            var result = await _addressRepository.GetAsync(AddressMonitorType.Balance, continuation, take);

            var addresses = result.items
                .Select(a => BitcoinAddress.Create(a.Address))
                .ToArray();

            var utxo = await _rpcClient.ListUnspentAsync(_settings.ConfirmationLevel, int.MaxValue, addresses);

            var balances = utxo.GroupBy(uc => uc.Address)
                .Select(g => new AddressBalance
                {
                    Asset = Asset.Zec,
                    Address = g.Key,
                    Balance = g.Aggregate(Money.Zero, (m, uc) => m + uc.Amount)
                })
                .ToArray();

            return(result.continuation, balances);
        }

        public async Task<bool> TryCreateObservableAddressAsync(AddressMonitorType monitorType, string address)
        {
            await _rpcClient.ImportAddressAsync(BitcoinAddress.Create(address), null, false);

            return await _addressRepository.CreateIfNotExistsAsync(monitorType, address);
        }

        public async Task<bool> TryDeleteObservableAddressAsync(AddressMonitorType monitorType, string address)
        {
            return await _addressRepository.DeleteIfExistAsync(monitorType, address));
        }

        public async Task<bool> IsObservableAsync(string category, string address)
        {
            var monitorType = category == TX_CATEGORY_RECEIVE 
                ? AddressMonitorType.To 
                : AddressMonitorType.From;

            var entity = await _addressRepository.GetAsync(monitorType, address);

            return entity != null;
        }

        public Money CalculateFee(TransactionBuilder transactionBuilder)
        {
            if (_settings.UseDefaultFee)
            {
                return Constants.DefaultFee;
            }
            else
            {
                var fee = transactionBuilder.EstimateFees(_feeRate);
                var min = Money.Coins(_settings.MinFee);
                var max = Money.Coins(_settings.MaxFee);

                return Money.Max(Money.Min(fee, max), min);
            }
        }

        public async Task<string[]> GetFromAddresses(string txHash, string toAddress)
        {
            var addresses = new List<string>();
            var network = BitcoinAddress.Create(toAddress).Network;
            var curr = await _rpcClient.GetRawTransactionAsync(uint256.Parse(txHash));

            foreach (var input in curr.Inputs)
            {
                var prev = await _rpcClient.GetRawTransactionAsync(input.PrevOut.Hash);
                var addr = prev.Outputs[input.PrevOut.N].ScriptPubKey.GetDestinationAddress(network);

                addresses.Add(addr.ToString());
            }

            return addresses.ToArray();
        }
    }
}
