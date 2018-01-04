using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.BlockchainSignService.Client;
using Lykke.Service.BlockchainSignService.Client.Models;
using Lykke.Service.Zcash.Api.Core.Domain.Insight;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Zcash.Api.Services;
using Moq;
using NBitcoin;
using NBitcoin.JsonConverters;
using NBitcoin.Zcash;
using Xunit;

namespace Lykke.Service.Zcash.Api.Tests
{
    public class BlockchainServiceTests
    {
        private Network network = ZcashNetworks.Testnet;
        private BitcoinSecret from = new BitcoinSecret("cP1myuzfN2C3Ac9GznMk75wnBtHJwZ4uNSvtk7CJd5CyMCXqpYHS");
        private BitcoinSecret to = new BitcoinSecret("cQV87Ee69NavGbV7ZG1KNAQ745X7nrPCnVPg8mECnvQmuJwZfq3d");
        private Money amount = Money.Coins(1);
        private ZcashApiSettings settings = new ZcashApiSettings()
        {
            FeePerByte = 1,
            MaxFee = 10000,
            MinFee = 0,
            UseDefaultFee = false
        };
        private Mock<IBlockchainSignServiceClient> signServiceClient = new Mock<IBlockchainSignServiceClient>();
        private Mock<IInsightClient> insightClient = new Mock<IInsightClient>();
        private IBlockchainService blockhainService;


        public BlockchainServiceTests()
        {
            ZcashNetworks.Register();

            signServiceClient
                .Setup(m => m.SignTransactionAsync(It.IsAny<SignRequestModel>()))
                .Returns((SignRequestModel r) =>
                {
                    var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(r.TransactionHex);

                    return Task.FromResult(new SignedTransactionModel
                    {
                        SignedTransaction = new TransactionBuilder()
                            .AddCoins(coins)
                            .AddKeys(from)
                            .SignTransaction(tx)
                            .ToHex()
                    });
                });

            insightClient
                .Setup(m => m.GetUtxoAsync(It.IsAny<BitcoinAddress>()))
                .Returns((BitcoinAddress a) => Task.FromResult(new Utxo[]
                {
                    new Utxo
                    {
                        Address = "tmA4rvdJU3HZ4ZUzZSjEUg7wbf1unbDBvGb",
                        Amount = 3M,
                        Satoshis = 300000000,
                        ScriptPubKey = "76a91403e105ea5dbb243cddada1ec31cebd92266cd22588ac",
                        TxId = "e9b284f552bde10839f7f4bb7e17624e53f8482f86d21e8c311c3576b403d249",
                        Vout = 0
                    }
                }));

            insightClient
                .Setup(m => m.SendTransactionAsync(It.IsAny<Transaction>()))
                .Returns((Transaction t) => Task.FromResult(new SendResult
                {
                    TxId = t.GetHash().ToString()
                }));

            blockhainService = new BlockchainService(signServiceClient.Object, insightClient.Object, settings);
        }

        [Fact]
        public async Task Transfer_ShouldBuildSignSendTransaction()
        {
            // Arrange

            // Act
            var hash = await blockhainService.TransferAsync(from.GetAddress(), to.GetAddress(), amount);

            // Assert
            signServiceClient.Verify(m => m.SignTransactionAsync(It.Is<SignRequestModel>(r => r.PublicAddresses.Single() == from.GetAddress().ToString())));
            insightClient.Verify(m => m.SendTransactionAsync(It.IsAny<Transaction>()));
        }

        [Fact]
        public async Task Transfer_ShouldThrow_IfSignIsInvalid()
        {
            // Arrange
            signServiceClient
                .Setup(m => m.SignTransactionAsync(It.IsAny<SignRequestModel>()))
                .Returns((SignRequestModel r) =>
                {
                    var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(r.TransactionHex);

                    return Task.FromResult(new SignedTransactionModel
                    {
                        SignedTransaction = tx.ToHex() // skip signing at all
                    });
                });

            // Act

            // Assert
            await Assert.ThrowsAnyAsync<Exception>(() => blockhainService.TransferAsync(from.GetAddress(), to.GetAddress(), amount));
        }
    }
}
