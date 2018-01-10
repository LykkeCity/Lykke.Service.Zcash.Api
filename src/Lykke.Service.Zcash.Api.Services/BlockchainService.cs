using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Balances;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using NBitcoin;
using NBitcoin.JsonConverters;
using NBitcoin.Zcash;
using ITxsRepository = Lykke.Service.Zcash.Api.Core.Services.ITransactionRepository;

namespace Lykke.Service.Zcash.Api.Services
{
    public class BlockchainService : IBlockchainService
    {
        private readonly IInsightClient _insightClient;
        private readonly ITxsRepository _txsRepository;
        private readonly ZcashApiSettings _settings;
        private readonly FeeRate _feeRate;

        static BlockchainService()
        {
            ZcashNetworks.Register();
        }

        public BlockchainService(IInsightClient insightClient, ITxsRepository txsRepository, ZcashApiSettings settings)
        {
            _insightClient = insightClient;
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

        public Money CalculateFee(TransactionBuilder txBuilder)
        {
            if (_settings.UseDefaultFee)
            {
                return Constants.DefaultFee;
            }
            else
            {
                var fee = txBuilder.EstimateFees(_feeRate);
                var min = Money.Coins(_settings.MinFee);
                var max = Money.Coins(_settings.MaxFee);

                return Money.Max(Money.Min(fee, max), min);
            }
        }




        public async Task<ITransaction> BuildUnsignedTxAsync(Guid operationId, BitcoinAddress from, BitcoinAddress to, Money amount, Asset asset, bool subtractFees)
        {
            var utxo = (await _insightClient.GetUtxoAsync(from))
                .OrderByDescending(x => x.Confirmations)
                .ThenBy(x => x.Vout)
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
                    var coin = new Coin(uint256.Parse(item.TxId), item.Vout, Money.Coins(item.Amount), from.ScriptPubKey);

                    builder.AddCoins(coin);

                    totalIn += coin.Amount;

                    var fee = CalculateFee(builder);
                    var totalOut = subtractFees
                        ? amount - fee
                        : amount + fee;

                    if (totalIn >= totalOut)
                    {
                        var tx = builder.SendFees(fee).BuildTransaction(sign: false);
                        var signContext = Serializer.ToString((tx: tx, coins: builder.FindSpentCoins(tx)));

                        return await _txsRepository.CreateAsync(operationId, from.ToString(), 
                            to.ToString(), asset.Id, amount.ToRoundTrip(asset), fee.ToRoundTrip(asset), signContext);
                    }
                }
            }

            throw new InvalidOperationException("Insufficient funds");
        }

        public async Task BroadcastTxAsync(ITransaction tx, string signedTransaction)
        {
            await _insightClient.SendTransactionAsync(signedTransaction);

            await _txsRepository.UpdateAsync(tx.OperationId, sentUtc: DateTime.Now.ToUniversalTime(), signedTransaction: signedTransaction);
        }

        public async Task<ITransaction> GetObservableTxAsync(Guid operationId)
        {
            return await _txsRepository.Get(operationId);
        }

        public async Task<IReadOnlyList<ITransaction>> GetObservableTxsByStateAsync(TransactionState state, int skip = 0, int take = 100)
        {
            return await _txsRepository.Get(state, skip, take);
        }

        public async Task UpdateObservableTxsAsync()
        {
            throw new NotImplementedException();
        }

        public async Task DeleteObservableTxsAsync(IEnumerable<Guid> operationIds)
        {
            await _txsRepository.DeleteAsync(operationIds);
        }

        public Task<IReadOnlyList<IBalance>> GetBalancesAsync(int skip = 0, int take = 100)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CreateObservableAddressAsync(string address)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteObservableAddressAsync(string address)
        {
            throw new NotImplementedException();
        }
    }
}
