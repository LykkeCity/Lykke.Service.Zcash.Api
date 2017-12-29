using System.Linq;
using NBitcoin;
using Xunit;

namespace Lykke.Service.Zcash.Api.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            NBitcoin.Zcash.ZcashNetworks.Register();


            var hash = uint256.Parse("e9b284f552bde10839f7f4bb7e17624e53f8482f86d21e8c311c3576b403d249");
            var coin = new Coin(new OutPoint(hash, 1), new TxOut(Money.Coins(3), BitcoinAddress.Create("tmA4rvdJU3HZ4ZUzZSjEUg7wbf1unbDBvGb")));

            var tx = Transaction.Parse("01000000037c29a35e2cf2097e883d31883e90a676d9bfd95b36bc65c5f26a7a6ee0209341000000006a47304402207a9c2c00ec27f56ba2cd7a5f93ddb54099bc9306505bc9a5fad4fb0baa7e7e6902201bf35e8e8d3c4597ae276c70ed2fff7fd87947ffbc20ce1aa6960ab4c1516a1101210212d0e07e3ad380457f25528499d34649d0bf9df8b9c312d4a1d417a278e02bf8feffffffee2ec4dde5f501feb47b3126a2c85cdf5184f9f055ba607997da6ba0c10c31a2000000006a4730440220645d523c8f81efa6c67213431620935a0502d07dc6570b0c43de8e0b10c3486002207d2e3cf7372e7cc0f394d9ee7d6b3a6d5a8be7b12c8537ab6ca972c0994ad58101210212d0e07e3ad380457f25528499d34649d0bf9df8b9c312d4a1d417a278e02bf8feffffff775fe2d64ff56f9f27075b85c8976736c3291add3b022d632f3fb370255006c6010000006a473044022055959a07c9e3329a6b1eae4fe1a04ed820e7396fb544da7dd96622d091495e0302207818c702fd0443a6179c42ff8d30d6ce5b6795b5f8ee009f2ad14587794d9986012102c6c6be4c0d25a6428456ecbf48727300c81a8a44cf8d42123c3b71da9dcca90afeffffff02445b1a00000000001976a914f6111327674b0073631ea6bebb4d5324fe8d729588ac00a3e111000000001976a91403e105ea5dbb243cddada1ec31cebd92266cd22588ac89aa0200");
            var txCoin = tx.Outputs.AsCoins().Skip(1).First();

        }
    }

}
