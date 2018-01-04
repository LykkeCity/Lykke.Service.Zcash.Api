using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainSignService.Client;
using Lykke.Service.BlockchainSignService.Client.Models;
using Lykke.Service.Zcash.Api.Core;
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
        private readonly IPendingEventRepository _pendingEventRepository;
        private readonly ZcashApiSettings _settings;
        private readonly FeeRate _feeRate;

        static BlockchainService()
        {
            ZcashNetworks.Register();
        }

        public BlockchainService(
            IBlockchainSignServiceClient signServiceClient, 
            IInsightClient insightClient, 
            IPendingEventRepository pendingEventRepository, 
            ZcashApiSettings settings)
        {
            _signServiceClient = signServiceClient;
            _insightClient = insightClient;
            _pendingEventRepository = pendingEventRepository;
            _settings = settings;
            _feeRate = new FeeRate(_settings.FeePerByte * 1024);
        }

        public async Task<string> CreateTransparentWalletAsync()
        {
            return (await _signServiceClient.CreateWalletAsync()).PublicAddress;
        }

        public async Task<string> TransferAsync(BitcoinAddress from, IDestination to, Money amount, params BitcoinAddress[] signers)
        {
            var utxo = await _insightClient.GetUtxoAsync(from);

            if (utxo != null && utxo.Any())
            {
                utxo = utxo
                    .OrderByDescending(x => x.Confirmations)
                    .ThenBy(x => x.Vout)
                    .ToArray();

                var total = Money.Zero;
                var fee = Money.Zero;
                var txBuilder = new TransactionBuilder().Send(to, amount).SetChange(from);

                foreach (var item in utxo)
                {
                    var coinAmount = Money.Coins(item.Amount);
                    var coin = new Coin(
                        new OutPoint(uint256.Parse(item.TxId), item.Vout),
                        new TxOut(coinAmount, from));

                    txBuilder.AddCoins(coin);
                    fee = CalcFee(txBuilder);
                    total += coinAmount;

                    if (total >= (amount + fee))
                    {
                        txBuilder.SendFees(fee);

                        var signBy = new[] { from }
                            .Concat(signers ?? Enumerable.Empty<BitcoinAddress>())
                            .ToArray();

                        return await SignAndSendAsync(txBuilder, signBy);
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

        public async Task<string> SignAndSendAsync(TransactionBuilder txBuilder, BitcoinAddress[] signers)
        {
            var tx = txBuilder.BuildTransaction(false);
            var txData = Serializer.ToString(new { tx, coins = txBuilder.FindSpentCoins(tx) });
            var txSignResult = await _signServiceClient.SignTransactionAsync(new SignRequestModel(signers.Select(a => a.ToString()), txData));
            var txSigned = Transaction.Parse(txSignResult.SignedTransaction);

            if (!txBuilder.Verify(txSigned, out TransactionPolicyError[] errors))
            {
                throw new InvalidOperationException($"Invalid transaction sign: {string.Join("; ", errors.Select(e => e.ToString()))}");
            }

            var txSent = await _insightClient.SendTransactionAsync(txSigned);

            return txSent.TxId;
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
