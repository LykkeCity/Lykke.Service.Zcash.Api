using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.History;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;
using Lykke.Service.Zcash.Api.Core.Domain.Settings;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Zcash.Api.Services;
using Lykke.Service.Zcash.Api.Services.Models;
using Moq;
using NBitcoin;
using NBitcoin.JsonConverters;
using NBitcoin.RPC;
using NBitcoin.Zcash;
using Xunit;

namespace Lykke.Service.Zcash.Api.Tests
{
    public class BlockchainServiceTests
    {
        private ILog log;
        private Network network = ZcashNetworks.Testnet;
        private BitcoinAddress depositWallet1 = BitcoinAddress.Create("tmRQYJ8KQg3qYjVtUEiZ5timrJ4N3AoRY1K");
        private BitcoinAddress depositWallet2 = BitcoinAddress.Create("tmA4rvdJU3HZ4ZUzZSjEUg7wbf1unbDBvGb");
        private BitcoinAddress depositWallet3 = BitcoinAddress.Create("tmL4JCMEFQW2YtxQptLbBJtH6dzowHouyxw");
        private BitcoinAddress hotWallet = BitcoinAddress.Create("tmBh9ifoTp5keLnCcVnpLzkuMiTLMoPaYdR");
        private Money amount = Money.Coins(1);
        private ZcashApiSettings settings = new ZcashApiSettings()
        {
            FeePerKb = 0.00000001M,
            MaxFee = 1M,
            MinFee = 0M,
            UseDefaultFee = true,
            ConfirmationLevel = 6,
            NetworkType = "zcash-testnet"
        };
        private Mock<IBlockchainReader> blockchainReader = new Mock<IBlockchainReader>();
        private Mock<IAddressRepository> addressRepository = new Mock<IAddressRepository>();
        private Mock<IOperationRepository> operationRepository = new Mock<IOperationRepository>();
        private Mock<IHistoryRepository> historyRepository = new Mock<IHistoryRepository>();
        private Mock<ISettingsRepository> settingsRepository = new Mock<ISettingsRepository>();

        private Utxo[] utxo = new Utxo[]
        {
            new Utxo
            {
                TxId = "c606502570b33f2f632d023bdd1a29c3366797c8855b07279f6ff54fd6e25f77",
                Vout = 0,
                Address = "tmRQYJ8KQg3qYjVtUEiZ5timrJ4N3AoRY1K",
                ScriptPubKey = "76a914ac23ad9a8017a24587d736377859184d8a2f5fca88ac",
                Amount = 3.00000000M,
                Confirmations = 13972
            },
            new Utxo
            {
                TxId = "6016f6dfd9e6fb8c7afa2f4fe9b9688654e5adc3627137269c698fe01d7d747b",
                Vout = 0,
                Address = "tmRQYJ8KQg3qYjVtUEiZ5timrJ4N3AoRY1K",
                ScriptPubKey = "76a914ac23ad9a8017a24587d736377859184d8a2f5fca88ac",
                Amount = 3.00000000M,
                Confirmations = 1
            },
            new Utxo
            {
                TxId = "683dfba8f312bfd2c0b675a5442dd66007d7e493c4a376bc2b332cf50adb5abd",
                Vout = 0,
                Address = "tmA4rvdJU3HZ4ZUzZSjEUg7wbf1unbDBvGb",
                ScriptPubKey = "76a91403e105ea5dbb243cddada1ec31cebd92266cd22588ac",
                Amount = 0.99996160M,
                Confirmations = 2411,
            },
            new Utxo
            {
                TxId = "afcbc8742bcecbb5bc7d5b24854619d26fb3944914f0a0d7a8d1d28af525c0c2",
                Vout = 1,
                Address = "tmL4JCMEFQW2YtxQptLbBJtH6dzowHouyxw",
                ScriptPubKey = "76a9147176f5d11e11c59a1248ef0bf0d6dadb2be1686188ac",
                Amount = 1.99990000M,
                Confirmations = 4724,
            }
        };

        private IBlockchainService blockhainService;

        static BlockchainServiceTests()
        {
            ZcashNetworks.Register();
        }

        public BlockchainServiceTests()
        {
            log = new LogToMemory();

            blockchainReader
                .Setup(x => x.ListUnspentAsync(It.IsAny<int>(), It.IsAny<string[]>()))
                .Returns((int level, string[] addrs) => Task.FromResult(utxo));

            blockhainService = new BlockchainService(log,
                blockchainReader.Object,
                addressRepository.Object,
                operationRepository.Object,
                historyRepository.Object,
                settingsRepository.Object,
                settings);
        }

        public RPCResponse RpcResponse(object response)
        {
            return RPCResponse.Load(response.ToJson().ToStream());
        }

        [Fact]
        public async Task Build_ShouldAddFee()
        {
            // Arrange

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), OperationType.SISO, Asset.Zec, false, (depositWallet1, hotWallet, Money.Coins(1)));
            var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(signContext);

            // Assert
            // only necessary utxo are used
            Assert.Single(tx.Inputs);
            // returns correct change and sends correct amount
            Assert.Collection(tx.Outputs,
                vout => {
                    Assert.True(vout.IsTo(depositWallet1));
                    Assert.Equal(Money.Coins(3) - Money.Coins(1) - Constants.DefaultFee, vout.Value);
                },
                vout => {
                    Assert.True(vout.IsTo(hotWallet));
                    Assert.Equal(Money.Coins(1), vout.Value);
                });
        }

        [Fact]
        public async Task Build_ShouldSubtractFee()
        {
            // Arrange

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), OperationType.SISO, Asset.Zec, true, (depositWallet1, hotWallet, Money.Coins(1)));
            var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(signContext);

            // Assert
            // only necessary utxo are used
            Assert.Single(tx.Inputs);
            // returns correct change and sends correct amount
            Assert.Collection(tx.Outputs,
                vout => {
                    Assert.True(vout.IsTo(depositWallet1));
                    Assert.Equal(Money.Coins(3) - Money.Coins(1), vout.Value);
                },
                vout => {
                    Assert.True(vout.IsTo(hotWallet));
                    Assert.Equal(Money.Coins(1) - Constants.DefaultFee, vout.Value);
                });
        }
    }
}
