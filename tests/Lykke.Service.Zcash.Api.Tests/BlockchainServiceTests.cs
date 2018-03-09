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
                Amount = 2.00000000M,
                Confirmations = 2411,
            },
            new Utxo
            {
                TxId = "afcbc8742bcecbb5bc7d5b24854619d26fb3944914f0a0d7a8d1d28af525c0c2",
                Vout = 1,
                Address = "tmL4JCMEFQW2YtxQptLbBJtH6dzowHouyxw",
                ScriptPubKey = "76a9147176f5d11e11c59a1248ef0bf0d6dadb2be1686188ac",
                Amount = 2.00000000M,
                Confirmations = 4724,
            },
            new Utxo
            {
                TxId = "afcbc8742bcecbb5bc7d5b24854619d26fb3944914f0a0d7a8d1d28af525c0c2",
                Vout = 2,
                Address = "tmBh9ifoTp5keLnCcVnpLzkuMiTLMoPaYdR",
                ScriptPubKey = "76a91415b6246e9b88867cdc3e14b9a5085813ca6d8b4888ac",
                Amount = 3.00000000M,
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

        public Money GetInputAmount(TxIn input)
        {
            return utxo
                .First(x => input.PrevOut.Hash.ToString() == x.TxId && input.PrevOut.N == x.Vout)
                .Money;
        }

        [Fact]
        public async Task Build_ShouldAddFee()
        {
            // Arrange
            var type = OperationType.SingleFromSingleTo;
            var subtractFee = false;
            var from = depositWallet1;
            var to = hotWallet;
            var amount = Money.Coins(1);

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from, to, amount));
            var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(signContext);

            // Assert
            // only necessary utxo are used
            var inputAmount = GetInputAmount(Assert.Single(tx.Inputs));
            // returns correct change and sends correct amount
            Assert.Contains(tx.Outputs, vout => vout.IsTo(from) && vout.Value == inputAmount - amount - Constants.DefaultFee);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(to) && vout.Value == amount);
        }

        [Fact]
        public async Task Build_ShouldAddFee_ForMultiFrom()
        {
            // Arrange
            var type = OperationType.MultiFromSingleTo;
            var subtractFee = false;
            var to = hotWallet;
            var from1 = depositWallet1;
            var from2 = depositWallet2;
            var amount1 = Money.Coins(1);
            var amount2 = Money.Coins(1);
            var fees = Constants.DefaultFee.Split(2).ToArray();

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from1, to, amount1), (from2, to, amount2));
            var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(signContext);

            // Assert
            // only necessary utxo are used
            Assert.Equal(2, tx.Inputs.Count);
            // returns correct change and sends correct amounts
            Assert.Contains(tx.Outputs, vout => vout.IsTo(from1) && vout.Value == GetInputAmount(tx.Inputs[0]) - amount1 - fees[0]);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(from2) && vout.Value == GetInputAmount(tx.Inputs[1]) - amount2 - fees[1]);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(to) && vout.Value == amount1 + amount2);
        }

        [Fact]
        public async Task Build_ShouldAddFee_ForMultiFromMultiTo()
        {
            // Arrange
            var type = OperationType.SingleFromMultiTo;
            var subtractFee = false;
            var from1 = depositWallet1;
            var from2 = hotWallet;
            var to1 = hotWallet;
            var to2 = depositWallet2;
            var amount1 = Money.Coins(1);
            var amount2 = Money.Coins(1);
            var fees = Constants.DefaultFee.Split(2).ToArray();

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from1, to1, amount1), (from2, to2, amount2));
            var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(signContext);

            // Assert
            // only necessary utxo are used
            Assert.Equal(2, tx.Inputs.Count);
            // returns correct change and sends correct amount
            Assert.Contains(tx.Outputs, vout => vout.IsTo(from1) && vout.Value == GetInputAmount(tx.Inputs[0]) - amount1 - fees[0]);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(from1) && vout.Value == GetInputAmount(tx.Inputs[1]) - amount2 - fees[1]);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(to1) && vout.Value == amount1);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(to2) && vout.Value == amount2);
        }

        [Fact]
        public async Task Build_ShouldAddFee_ForMultiTo()
        {
            // Arrange
            var type = OperationType.SingleFromMultiTo;
            var subtractFee = false;
            var from = hotWallet;
            var to1 = depositWallet1;
            var to2 = depositWallet2;
            var amount1 = Money.Coins(1);
            var amount2 = Money.Coins(1);

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from, to1, amount1), (from, to2, amount2));
            var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(signContext);

            // Assert
            // only necessary utxo are used
            var inputAmount = GetInputAmount(Assert.Single(tx.Inputs));
            // returns correct change and sends correct amount
            Assert.Contains(tx.Outputs, vout => vout.IsTo(from) && vout.Value == inputAmount - amount1 - amount2 - Constants.DefaultFee);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(to1) && vout.Value == amount1);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(to2) && vout.Value == amount2);
        }

        [Fact]
        public async Task Build_ShouldSubtractFee()
        {
            // Arrange
            var type = OperationType.SingleFromSingleTo;
            var subtractFee = true;
            var from = depositWallet1;
            var to = hotWallet;
            var amount = Money.Coins(1);

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from, to, amount));
            var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(signContext);

            // Assert
            // only necessary utxo are used
            var inputAmount = GetInputAmount(Assert.Single(tx.Inputs));
            // returns correct change and sends correct amount
            Assert.Contains(tx.Outputs, vout => vout.IsTo(depositWallet1) && vout.Value == inputAmount - amount);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(hotWallet) && vout.Value == amount - Constants.DefaultFee);
        }

        [Fact]
        public async Task Build_ShouldSubtractFee_ForMultiFrom()
        {
            // Arrange
            var type = OperationType.MultiFromSingleTo;
            var subtractFee = true;
            var to = hotWallet;
            var from1 = depositWallet1;
            var from2 = depositWallet2;
            var amount1 = Money.Coins(1);
            var amount2 = Money.Coins(1);

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from1, to, amount1), (from2, to, amount2));
            var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(signContext);

            // Assert
            // only necessary utxo are used
            Assert.Equal(2, tx.Inputs.Count);
            // returns correct change and sends correct amounts
            Assert.Contains(tx.Outputs, vout => vout.IsTo(from1) && vout.Value == GetInputAmount(tx.Inputs[0]) - amount1);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(from2) && vout.Value == GetInputAmount(tx.Inputs[1]) - amount2);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(to) && vout.Value == amount1 + amount2 - Constants.DefaultFee);
        }

        [Fact]
        public async Task Build_ShouldSubtractFee_ForMultiFromMultiTo()
        {
            // Arrange
            var type = OperationType.SingleFromMultiTo;
            var subtractFee = true;
            var from1 = depositWallet1;
            var from2 = hotWallet;
            var to1 = hotWallet;
            var to2 = depositWallet2;
            var amount1 = Money.Coins(1);
            var amount2 = Money.Coins(1);
            var fees = Constants.DefaultFee.Split(2).ToArray();

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from1, to1, amount1), (from2, to2, amount2));
            var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(signContext);

            // Assert
            // only necessary utxo are used
            Assert.Equal(2, tx.Inputs.Count);
            // returns correct change and sends correct amount
            Assert.Contains(tx.Outputs, vout => vout.IsTo(from1) && vout.Value == GetInputAmount(tx.Inputs[0]) - amount1);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(from1) && vout.Value == GetInputAmount(tx.Inputs[1]) - amount2);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(to1) && vout.Value == amount1 - fees[0]);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(to2) && vout.Value == amount2 - fees[1]);
        }

        [Fact]
        public async Task Build_ShouldSubtractFee_ForMultiTo()
        {
            // Arrange
            var type = OperationType.SingleFromMultiTo;
            var subtractFee = true;
            var from = hotWallet;
            var to1 = depositWallet1;
            var to2 = depositWallet2;
            var amount1 = Money.Coins(1);
            var amount2 = Money.Coins(1);
            var fees = Constants.DefaultFee.Split(2).ToArray();

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from, to1, amount1), (from, to2, amount2));
            var (tx, coins) = Serializer.ToObject<(Transaction, ICoin[])>(signContext);

            // Assert
            // only necessary utxo are used
            var inputAmount = GetInputAmount(Assert.Single(tx.Inputs));
            // returns correct change and sends correct amount

            Assert.Contains(tx.Outputs, vout => vout.IsTo(from) && vout.Value == inputAmount - amount1 - amount2);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(to1) && vout.Value == amount1 - fees[0]);
            Assert.Contains(tx.Outputs, vout => vout.IsTo(to2) && vout.Value == amount2 - fees[1]);
        }

        [Theory]
        [InlineData(7)] // not enough for operation
        [InlineData(6)] // not enough for fee
        public async Task Build_ShouldThrowNotEnoughFunds(decimal amountValue)
        {
            // Arrange
            var type = OperationType.SingleFromSingleTo;
            var subtractFee = false;
            var from = depositWallet1;
            var to = hotWallet;
            var amount = Money.Coins(amountValue);

            // Act

            // Assert
            await Assert.ThrowsAsync<NotEnoughFundsException>(async () =>
                await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from, to, amount)));
        }

        public async Task Test()
        {
            var (transaction, coins) = Serializer.ToObject<(Transaction, ICoin[])>("ewogICJpdGVtMSI6ICIwMTAwMDAwMDAxOWQ2MzQzNjJlNGMzNjcxYTEzOWMzMzUzYjY3NzI2NzJlMmMxMWExMWFmNGI2M2VjMzhmMDYyNmQ0NGEzYzRlNjAwMDAwMDAwNmI0ODMwNDUwMjIxMDA4Yzg2YTMzMDkyZGNlOWNkMTJmNGRkMjRkYThjNmQ3YzdlOWZiMTY5MTEzODY4NzhjOTQ1Y2VjN2ViNjM3YmNmMDIyMDc2ZDAyNTViNTZjN2RkODU2ZmI1OGJlNWI4MDE0Zjk5M2M5NmJkZmY2YTYyYzAyZTgxODRlMTUwMDI2MjRiNDkwMTIxMDIyMzA2MTJlZDlmZDg4NjVhMDExNTY4NWNiYzY2MDNkNWQxYmYxM2E4ZTkyZWI3MGRiMzEyYWYxNjYwNzQzMGZhZmZmZmZmZmYwMjM0N2JkODExMDAwMDAwMDAxOTc2YTkxNDIwZWM0MDFmMmQxZTI3ZjEyZGM1MjRjNmY2ODMwYTU4OWNjOTM4Yjk4OGFjNTY3ZDAxMDAwMDAwMDAwMDE5NzZhOTE0NTk1ZmJmZGVkYzk4NDU4Y2VlOTUyYzRjMTYwZTNjNzRjNGNhNzNmZjg4YWMwMDAwMDAwMCIsCiAgIml0ZW0yIjogWwogICAgewogICAgICAidHJhbnNhY3Rpb25JZCI6ICJlNmM0YTM0NDZkNjJmMDM4ZWM2MzRiYWYxMTFhYzFlMjcyMjY3N2I2NTMzMzljMTMxYTY3YzNlNDYyNDM2MzlkIiwKICAgICAgImluZGV4IjogMCwKICAgICAgInZhbHVlIjogMjk5NDk5OTkwLAogICAgICAic2NyaXB0UHViS2V5IjogIjc2YTkxNDIwZWM0MDFmMmQxZTI3ZjEyZGM1MjRjNmY2ODMwYTU4OWNjOTM4Yjk4OGFjIiwKICAgICAgInJlZGVlbVNjcmlwdCI6IG51bGwKICAgIH0KICBdCn0=".Base64ToString());
        }
    }
}
