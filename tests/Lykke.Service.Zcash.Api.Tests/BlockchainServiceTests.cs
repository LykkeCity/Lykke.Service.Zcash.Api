using System;
using System.Linq;
using System.Threading.Tasks;
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
using Newtonsoft.Json;
using Xunit;

namespace Lykke.Service.Zcash.Api.Tests
{
    public class BlockchainServiceTests
    {
        private ILog log;
        private string depositWallet1 = "tmRQYJ8KQg3qYjVtUEiZ5timrJ4N3AoRY1K";
        private string depositWallet2 = "tmA4rvdJU3HZ4ZUzZSjEUg7wbf1unbDBvGb";
        private string depositWallet3 = "tmL4JCMEFQW2YtxQptLbBJtH6dzowHouyxw";
        private string hotWallet = "tmBh9ifoTp5keLnCcVnpLzkuMiTLMoPaYdR";
        private decimal amount = 1M;
        private decimal relayFee = 0.000001m;
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

        public BlockchainServiceTests()
        {
            log = new LogToMemory();

            blockchainReader
                .Setup(x => x.ListUnspentAsync(It.IsAny<int>(), It.IsAny<string[]>()))
                .Returns((int level, string[] addrs) => Task.FromResult(utxo));

            blockchainReader
                .Setup(x => x.GetInfoAsync())
                .Returns(() => Task.FromResult(new Info { RelayFee = relayFee }));

            blockchainReader
                .Setup(x => x.DecodeRawTransaction(It.IsAny<string>()))
                .Returns((string hex) => Task.FromResult(new RawTransaction()));

            blockhainService = new BlockchainService(log,
                blockchainReader.Object,
                addressRepository.Object,
                operationRepository.Object,
                historyRepository.Object,
                settingsRepository.Object,
                settings);
        }

        [Fact]
        public async Task Build_ShouldAddFee()
        {
            // Arrange
            var type = OperationType.SingleFromSingleTo;
            var subtractFee = false;
            var from = depositWallet1;
            var to = hotWallet;
            var amount = 1m;

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from, to, amount));
            var (tx, spent) = JsonConvert.DeserializeObject<(string, Utxo[])>(signContext.context);

            // Assert
            // only necessary utxo are used
            var inputAmount = Assert.Single(spent).Amount;
            // returns correct change and sends correct amount
            Assert.Contains(signContext.outputs, vout => vout.Key == from && vout.Value == inputAmount - amount - Constants.DefaultFee);
            Assert.Contains(signContext.outputs, vout => vout.Key == to && vout.Value == amount);
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
            var amount1 = 0.5m;
            var amount2 = 1.5m;
            var fee1 = Constants.DefaultFee / 4;
            var fee2 = Constants.DefaultFee / 4 * 3;

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from1, to, amount1), (from2, to, amount2));
            var (tx, spent) = JsonConvert.DeserializeObject<(string, Utxo[])>(signContext.context);

            // Assert
            // only necessary utxo are used
            Assert.Equal(2, spent.Length);
            // returns correct change and sends correct amounts
            Assert.Contains(signContext.outputs, vout => vout.Key == from1 && vout.Value == spent.First(x => x.Address == from1).Amount - amount1 - fee1);
            Assert.Contains(signContext.outputs, vout => vout.Key == from2 && vout.Value == spent.First(x => x.Address == from2).Amount - amount2 - fee2);
            Assert.Contains(signContext.outputs, vout => vout.Key == to && vout.Value == amount1 + amount2);
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
            var amount1 = 0.5m; 
            var amount2 = 1.5m;
            var fee1 = Constants.DefaultFee / 4;
            var fee2 = Constants.DefaultFee / 4 * 3;

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from1, to1, amount1), (from2, to2, amount2));
            var (tx, spent) = JsonConvert.DeserializeObject<(string, Utxo[])>(signContext.context);

            // Assert
            // only necessary utxo are used
            Assert.Equal(2, spent.Length);
            // returns correct change and sends correct amount
            // IMPORTANT: from2 == to1, build optimizes outputs for such cases
            Assert.Contains(signContext.outputs, vout => vout.Key == from1 && vout.Value == spent.First(x => x.Address == from1).Amount - amount1 - fee1);
            Assert.Contains(signContext.outputs, vout => vout.Key == from2 && vout.Value == spent.First(x => x.Address == from2).Amount - amount2 - fee2 + amount1);
            Assert.Contains(signContext.outputs, vout => vout.Key == to2 && vout.Value == amount2);
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
            var amount1 = 1m;
            var amount2 = 1m;

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from, to1, amount1), (from, to2, amount2));
            var (tx, spent) = JsonConvert.DeserializeObject<(string, Utxo[])>(signContext.context);

            // Assert
            // only necessary utxo are used
            var inputAmount = Assert.Single(spent).Amount;
            // returns correct change and sends correct amount
            Assert.Contains(signContext.outputs, vout => vout.Key == from && vout.Value == inputAmount - amount1 - amount2 - Constants.DefaultFee);
            Assert.Contains(signContext.outputs, vout => vout.Key == to1 && vout.Value == amount1);
            Assert.Contains(signContext.outputs, vout => vout.Key == to2 && vout.Value == amount2);
        }

        [Fact]
        public async Task Build_ShouldSubtractFee()
        {
            // Arrange
            var type = OperationType.SingleFromSingleTo;
            var subtractFee = true;
            var from = depositWallet1;
            var to = hotWallet;
            var amount = 1m;

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from, to, amount));
            var (tx, spent) = JsonConvert.DeserializeObject<(string, Utxo[])>(signContext.context);

            // Assert
            // only necessary utxo are used
            var inputAmount = Assert.Single(spent).Amount;
            // returns correct change and sends correct amount
            Assert.Contains(signContext.outputs, vout => vout.Key == depositWallet1 && vout.Value == inputAmount - amount);
            Assert.Contains(signContext.outputs, vout => vout.Key == hotWallet && vout.Value == amount - Constants.DefaultFee);
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
            var amount1 = 1m;
            var amount2 = 1m;

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from1, to, amount1), (from2, to, amount2));
            var (tx, spent) = JsonConvert.DeserializeObject<(string, Utxo[])>(signContext.context);

            // Assert
            // only necessary utxo are used
            Assert.Equal(2, spent.Length);
            // returns correct change and sends correct amounts
            Assert.Contains(signContext.outputs, vout => vout.Key == from1 && vout.Value == spent.First(x => x.Address == from1).Amount - amount1);
            Assert.Contains(signContext.outputs, vout => vout.Key == from2 && vout.Value == spent.First(x => x.Address == from2).Amount - amount2);
            Assert.Contains(signContext.outputs, vout => vout.Key == to && vout.Value == amount1 + amount2 - Constants.DefaultFee);
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
            var amount1 = 0.5m;
            var amount2 = 1.5m;
            var fee1 = Constants.DefaultFee / 4;
            var fee2 = Constants.DefaultFee / 4 * 3;

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from1, to1, amount1), (from2, to2, amount2));
            var (tx, spent) = JsonConvert.DeserializeObject<(string, Utxo[])>(signContext.context);

            // Assert
            // only necessary utxo are used
            Assert.Equal(2, spent.Length);
            // returns correct change and sends correct amount
            // IMPORTANT: from2 == to1, build optimizes outputs for such cases
            Assert.Contains(signContext.outputs, vout => vout.Key == from1 && vout.Value == spent.First(x => x.Address == from1).Amount - amount1);
            Assert.Contains(signContext.outputs, vout => vout.Key == from2 && vout.Value == spent.First(x => x.Address == from2).Amount - amount2 + amount1 - fee1);
            Assert.Contains(signContext.outputs, vout => vout.Key == to2 && vout.Value == amount2 - fee2);
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
            var amount1 = 0.5m;
            var amount2 = 1.5m;
            var fee1 = Constants.DefaultFee / 4;
            var fee2 = Constants.DefaultFee / 4 * 3;

            // Act
            var signContext = await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from, to1, amount1), (from, to2, amount2));
            var (tx, spent) = JsonConvert.DeserializeObject<(string, Utxo[])>(signContext.context);

            // Assert
            // only necessary utxo are used
            var inputAmount = Assert.Single(spent).Amount;
            // returns correct change and sends correct amount
            Assert.Contains(signContext.outputs, vout => vout.Key == from && vout.Value == inputAmount - amount1 - amount2);
            Assert.Contains(signContext.outputs, vout => vout.Key == to1 && vout.Value == amount1 - fee1);
            Assert.Contains(signContext.outputs, vout => vout.Key == to2 && vout.Value == amount2 - fee2);
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
            var amount = amountValue;

            // Act

            // Assert
            await Assert.ThrowsAsync<BuildTransactionException>(async () =>
                await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from, to, amount)));
        }
        
        [Theory]
        [InlineData(0.00009000)] // a bit less than default fee (result amount is less than 0)
        [InlineData(0.00010000)] // default fee (result amount is 0)
        [InlineData(0.00010000 + 0.182 * 3 * 0.000001)] // default fee + relay fee (result amount is relayFee)
        public async Task Build_ShouldThrowDust(decimal amount)
        {
            // Arrange
            var type = OperationType.SingleFromSingleTo;
            var subtractFee = true;
            var from = depositWallet1;
            var to = hotWallet;

            // Act

            // Assert
            await Assert.ThrowsAsync<BuildTransactionException>(async () =>
                await blockhainService.BuildAsync(Guid.NewGuid(), type, Asset.Zec, subtractFee, (from, to, amount)));
        }
    }
}
