using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainSignService.Client;
using Lykke.Service.BlockchainSignService.Client.Models;
using Lykke.Service.Zcash.Api.Core;
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
        private readonly IInsightsClient _insightService;
        private readonly ZcashApiSettings _settings;
        private readonly FeeRate _feeRate;

        static BlockchainService()
        {
            ZcashNetworks.Register();
        }

        public BlockchainService(IBlockchainSignServiceClient signServiceClient, IInsightsClient insightService, ZcashApiSettings settings)
        {
            _signServiceClient = signServiceClient;
            _insightService = insightService;
            _settings = settings;
            _feeRate = new FeeRate(_settings.FeePerByte * 1024);
        }

        public async Task<string> CreateTransparentWalletAsync()
        {
            return (await _signServiceClient.CreateWalletAsync()).PublicAddress;
        }

        public async Task TransferAsync(BitcoinAddress from, IDestination to, Money amount, params BitcoinAddress[] signers)
        {
            // request UTXO
            var utxo = _insightService.GetUtxo(from)
                .OrderByDescending(x => x.Confirmations)
                .ThenBy(x => x.Vout)
                .ToArray();

            if (utxo.Any())
            {
                var total = Money.Zero;
                var fee = Money.Zero;
                var txBuilder = new TransactionBuilder().Send(to, amount).SetChange(from);

                foreach (var item in utxo)
                {
                    var value = Money.Coins(item.Amount);

                    txBuilder.AddCoins(new Coin(new OutPoint(uint256.Parse(item.TxId), item.Vout), new TxOut(value, from)));
                    fee = CalcFee(txBuilder);
                    total += value;

                    if (total >= amount + fee)
                    {
                        await SendTransactionAsync(txBuilder.SendFees(fee), from);
                    }
                }
            }

            throw new InvalidOperationException("Insufficient funds");
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

        public async Task SendTransactionAsync(TransactionBuilder txBuilder, BitcoinAddress from)
        {
            var tx = txBuilder.BuildTransaction(false);
            var txData = Serializer.ToString(new { tx, coins = txBuilder.FindSpentCoins(tx) });
            var txSignModel = await _signServiceClient.SignTransactionAsync(new SignRequestModel(new[] { from.ToString() }, txData));
            var txSigned = Transaction.Parse(txSignModel.SignedTransaction);

            if (!txBuilder.Verify(txSigned, out TransactionPolicyError[] errors))
            {
                throw new InvalidOperationException($"Invalid transaction sign: {string.Join("; ", errors.Select(e => e.ToString()))}");
            }

            await _insightService.Broadcast(txSigned);
        }

        public Money CalcFee(TransactionBuilder txBuilder)
        {
            if (_settings.UseDefaultFee)
            {
                return Constants.DefaultFee;
            }
            else
            {
                var fee = txBuilder.EstimateFees(_feeRate);
                var min = Money.Satoshis(_settings.MinFee);
                var max = Money.Satoshis(_settings.MaxFee);

                return Money.Max(Money.Min(fee, max), min);
            }
        }
    }
}
