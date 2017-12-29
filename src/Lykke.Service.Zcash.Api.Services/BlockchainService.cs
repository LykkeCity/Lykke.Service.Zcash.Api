using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainSignService.Client;
using Lykke.Service.BlockchainSignService.Client.Models;
using Lykke.Service.Zcash.Api.Core.Services;
using NBitcoin;
using NBitcoin.JsonConverters;
using NBitcoin.Policy;
using NBitcoin.Zcash;

namespace Lykke.Service.Zcash.Api.Services
{
    public class BlockchainService : IBlockchainService
    {
        private readonly IBlockchainSignServiceClient _signServiceClient;
        private readonly IInsightService _insightService;

        static BlockchainService()
        {
            ZcashNetworks.Register();
        }

        public BlockchainService(IBlockchainSignServiceClient signServiceClient, IInsightService insightService)
        {
            _signServiceClient = signServiceClient;
            _insightService = insightService;
        }

        public async Task<string> CreateTransparentWalletAsync()
        {
            return (await _signServiceClient.CreateWalletAsync()).PublicAddress;
        }

        public async Task Transfer(BitcoinAddress from, IDestination to, Money amount, params BitcoinAddress[] signers)
        {
            // request UTXO
            var utxo = _insightService.GetUtxo(from);

            if (utxo == null || utxo.Length == 0)
            {
                throw new InvalidOperationException("Insufficient funds");
            }

            var coins = new List<ICoin>();
            var total = Money.Zero;
            var fee = Money.Zero;

            foreach (var item in utxo)
            {
                coins.Add(new Coin(new OutPoint(uint256.Parse(item.TxId), item.Vout), new TxOut(Money.Coins(item.Amount), from)));
                total += Money.Coins(item.Amount);

                // 

                if (total > amount)
                {
                    break;
                }
            }

            var txBuilder = new TransactionBuilder();

            var tx = txBuilder
                .AddCoins(coins.ToArray())
                .Send(to, amount)
                .SetChange(from)
                .SendFees(fee)
                .BuildTransaction(false);

            var spentCoins = txBuilder.FindSpentCoins(tx);

            var txData = Serializer.ToString(new { tx, coins });

            var txSignModel = await _signServiceClient.SignTransactionAsync(new SignRequestModel(new[] { from.ToString() }, txData));

            var signedTx = Transaction.Parse(txSignModel.SignedTransaction);

            // Assert
            if (!txBuilder.Verify(signedTx, out TransactionPolicyError[] errors))
            {
                throw new InvalidOperationException($"Invalid transaction sign: {string.Join(" ", errors.Select(e => e.ToString()))}");
            }

            await _insightService.Broadcast(signedTx);
        }

        public bool IsValidAddress(string address)
        {
            try
            {
                return BitcoinAddress.Create(address) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
