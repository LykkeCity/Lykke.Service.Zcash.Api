using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainSignService.Client;
using Lykke.Service.Zcash.Api.Core.Services;
using NBitcoin;
using NBitcoin.Zcash;

namespace Lykke.Service.Zcash.Api.Services
{
    public class WalletService : IWalletService
    {
        private readonly IBlockchainSignServiceClient _signServiceClient;

        static WalletService()
        {
            ZcashNetworks.Register();
        }

        public WalletService(IBlockchainSignServiceClient signServiceClient)
        {
            _signServiceClient = signServiceClient;
        }

        public async Task<string> CreateTransparentWalletAsync()
        {
            return (await _signServiceClient.CreateWalletAsync()).PublicAddress;
        }

        public async Task<string> PayOut(string from, string to, ulong amount, string[] signers)
        {
            var fromAddress = new BitcoinPubKeyAddress(from);
            var toAddress = new BitcoinPubKeyAddress(to);
            var txPrev = new Transaction();
            var txBuilder = new TransactionBuilder();

            var tx = txBuilder
                .AddCoins(txPrev.Outputs.AsCoins().ToArray())
                .Send(toAddress, Money.Satoshis(amount))
                .SetChange(fromAddress)
                .SendFees(Money.Cents(1))
                .BuildTransaction(false);
            var spentCoins = txBuilder.FindSpentCoins(tx);


        }

        public bool IsValid(string address)
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
