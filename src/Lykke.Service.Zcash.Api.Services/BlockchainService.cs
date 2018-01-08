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
        private readonly IBlockchainSignServiceClient _signServiceClient;
        private readonly IInsightClient _insightClient;
        private readonly ZcashApiSettings _settings;
        private readonly FeeRate _feeRate;

        static BlockchainService()
        {
            ZcashNetworks.Register();
        }

        public BlockchainService(
            IBlockchainSignServiceClient signServiceClient, 
            IInsightClient insightClient,
            ZcashApiSettings settings)
        {
            _signServiceClient = signServiceClient;
            _insightClient = insightClient;
            _settings = settings;
            _feeRate = new FeeRate(_settings.FeeRate.ToZec());
        }

        public async Task<string> CreateTransparentWalletAsync()
        {
            return (await _signServiceClient.CreateWalletAsync()).PublicAddress;
        }

        public async Task<string> BuildTransactionAsync(BitcoinAddress from, BitcoinAddress to, Money amount, bool subtractFees = false, decimal feeFactor = decimal.One)
        {
            var utxo = await _insightClient.GetUtxoAsync(from);

            if (utxo != null && utxo.Any())
            {
                utxo = utxo
                    .OrderByDescending(x => x.Confirmations)
                    .ThenBy(x => x.Vout)
                    .ToArray();

                var totalIn = Money.Zero;

                var builder = new TransactionBuilder().Send(to, amount).SetChange(from);
                
                if (subtractFees)
                {
                    builder.SubtractFees();
                }

                foreach (var item in utxo)
                {
                    var coin = new Coin(uint256.Parse(item.TxId), item.Vout, item.Amount.ToZec(), from.ScriptPubKey);

                    builder.AddCoins(coin);

                    totalIn += coin.Amount;

                    var fee = (CalcFee(builder) * feeFactor).ToZec();

                    var totalOut = subtractFees 
                        ? amount - fee
                        : amount + fee;

                    if (totalIn >= totalOut)
                    {
                        var tx = builder.SendFees(fee).BuildTransaction(false);

                        if (_settings.EnableRbf)
                        {
                            foreach (var input in tx.Inputs)
                            {
                                input.Sequence = (uint)(feeFactor * 100);
                            }
                        }

                        return Serializer.ToString((tx: tx, coins: builder.FindSpentCoins(tx)));
                    }
                }
            }

            throw new InvalidOperationException("Insufficient funds");
        }

        public async Task<string> BroadcastTransactionAsync(Transaction tx)
        {
            return (await _insightClient.SendTransactionAsync(tx)).TxId;
        }

        public bool IsValidAddress(string address, out BitcoinAddress bitcoinAddress)
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

        public decimal CalcFee(TransactionBuilder txBuilder)
        {
            if (_settings.UseDefaultFee)
            {
                return Constants.DefaultFee;
            }
            else
            {
                var fee = txBuilder.EstimateFees(_feeRate).ToUnit(Asset.Zec.Unit);
                var min = _settings.MinFee;
                var max = _settings.MaxFee;

                return Math.Max(Math.Min(fee, max), min);
            }
        }
    }
}
