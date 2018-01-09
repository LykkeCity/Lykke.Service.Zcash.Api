using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainSignService.Client;
using Lykke.Service.BlockchainSignService.Client.Models;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Events;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using NBitcoin;
using NBitcoin.JsonConverters;
using NBitcoin.Policy;
using NBitcoin.Zcash;

namespace Lykke.Service.Zcash.Api.Services
{
    public class BlockchainService : IBlockchainService
    {
        private readonly IInsightClient _insightClient;
        private readonly ZcashApiSettings _settings;
        private readonly FeeRate _feeRate;

        static BlockchainService()
        {
            ZcashNetworks.Register();
        }

        public BlockchainService(IInsightClient insightClient, ZcashApiSettings settings)
        {
            _insightClient = insightClient;
            _settings = settings;
            _feeRate = new FeeRate(Money.Coins(_settings.FeePerKb));
        }

        public async Task<string> BuildTransactionAsync(BitcoinAddress from, BitcoinAddress to, Money amount, bool subtractFees = false)
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
                        var tx = builder.SendFees(fee).BuildTransaction(false);
                        var context = Serializer.ToString((tx: tx, coins: builder.FindSpentCoins(tx)));

                        return context;
                    }
                }
            }

            throw new InvalidOperationException("Insufficient funds");
        }

        public async Task<string> BroadcastTransactionAsync(Transaction tx)
        {
            return (await _insightClient.SendTransactionAsync(tx)).TxId;
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
    }
}
