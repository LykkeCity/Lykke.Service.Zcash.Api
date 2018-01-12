using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Zcash.Api.Services.Models;
using NBitcoin;
using NBitcoin.JsonConverters;
using NBitcoin.RPC;
using NBitcoin.Zcash;
using ITxsRepository = Lykke.Service.Zcash.Api.Core.Services.ITransactionRepository;

namespace Lykke.Service.Zcash.Api.Services
{
    public class BlockchainService : IBlockchainService
    {
        private readonly RPCClient _rpcClient;
        private readonly IAddressRepository _addressRepository;
        private readonly ITxsRepository _txsRepository;
        private readonly ZcashApiSettings _settings;
        private readonly FeeRate _feeRate;

        static BlockchainService()
        {
            ZcashNetworks.Register();
        }

        public BlockchainService(RPCClient rpcClient, IAddressRepository addressRepository, ITxsRepository txsRepository, ZcashApiSettings settings)
        {
            _rpcClient = rpcClient;
            _addressRepository = addressRepository;
            _txsRepository = txsRepository;
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

        public async Task<ITransaction> BuildNotSignedTxAsync(Guid operationId, BitcoinAddress from, BitcoinAddress to, Money amount, Asset asset, bool subtractFees)
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

                        return await _txsRepository.CreateAsync(operationId, from.ToString(), 
                            to.ToString(), asset.Id, amount.ToUnit(asset.Unit), fee.ToUnit(asset.Unit), signContext);
                    }
                }
            }

            throw new InvalidOperationException("Insufficient funds");
        }

        public async Task BroadcastTxAsync(ITransaction tx, Transaction transaction)
        {
            try
            {
                await _rpcClient.SendRawTransactionAsync(transaction);

                await _txsRepository.UpdateAsync(tx.OperationId, signedTransaction: transaction.ToHex(),
                    sentUtc: DateTime.Now.ToUniversalTime(), 
                    hash: transaction.GetHash().ToString());
            }
            catch (Exception ex)
            {
                await _txsRepository.UpdateAsync(tx.OperationId, signedTransaction: transaction.ToHex(),
                    failedUtc: DateTime.Now.ToUniversalTime(), 
                    error: ex.ToString());
            }
        }

        public async Task<ITransaction> GetOperationalTxAsync(Guid operationId)
        {
            return await _txsRepository.GetAsync(operationId);
        }

        public async Task<PagedResult<ITransaction>> GetOperationalTxsByStateAsync(TransactionState state, string continuation = null, int take = 100)
        {
            return await _txsRepository.GetAsync(state, continuation, take);
        }

        public async Task HandleTxsAsync()
        {
            var rpc = await _rpcClient.SendCommandAsync(RPCOperations.listsinceblock, "", _settings.ConfirmationLevel, true);

            rpc.ThrowIfError();

            var res = rpc.Result.ToObject<ListedTransactionResult>();

            foreach (var t in res.Transactions.Where(t => t.Confirmations >= _settings.ConfirmationLevel))
            {
                var txDateTime = DateTimeOffset.FromUnixTimeSeconds(t.BlockTime);

                if (sent.TryGetValue(t.TxId, out var tx))
                {
                    _txsRepository.UpdateAsync(TransactionState.Completed, tx.OperationId, completedUtc: txDateTime.UtcDateTime);
                }

                if (t.Category == "receive" && toAddresses.TryGetValue(t.Address, out var address))
                {
                    _historyRepository.CreateAsync();
                }

                if (t.Category == "send" && fromAddresses.TryGetValue(t.Address, out var address))
                {
                    _historyRepository.CreateAsync();
                }
            }

            _blocksRepository.SaveLastProcessedBlockHash(res.LastBlock);
        }

        public async Task DeleteOperationalTxsAsync(IEnumerable<Guid> operationIds)
        {
            await _txsRepository.DeleteAsync(operationIds);
        }

        public async Task<PagedResult<AddressBalance>> GetBalancesAsync(string continuation = null, int take = 100)
        {
            var result = await _addressRepository.GetObservableAddresses(AddressMonitorType.Balance, continuation, take);

            var addresses = result.Items
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

            return new PagedResult<AddressBalance>()
            {
                Continuation = result.Continuation,
                Items = balances
            };
        }

        public async Task<bool> TryCreateObservableAddressAsync(AddressMonitorType monitorType, string address)
        {
            var entity = _addressRepository.GetAsync(monitorType, address);

            if (entity == null)
            {
                await _rpcClient.ImportAddressAsync(BitcoinAddress.Create(address), null, false);
                await _addressRepository.CreateAsync(monitorType, address);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> TryDeleteObservableAddressAsync(AddressMonitorType monitorType, string address)
        {
            var entity = _addressRepository.GetAsync(monitorType, address);

            if (entity == null)
            {
                await _addressRepository.DeleteAsync(monitorType, address);
                return true;
            }
            else
            {
                return false;
            }
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
    }
}
