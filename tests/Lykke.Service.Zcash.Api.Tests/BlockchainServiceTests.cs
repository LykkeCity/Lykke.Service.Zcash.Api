﻿using System;
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
                .Returns((Transaction t) => Task.FromResult(new SendTransactionResult
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

        [Fact]
        public void ParseBlock()
        {
            var rawBlock = "04000000cfa0d9d8f23bbc25fdb1699842a86630b2ad644b1510b51f809efb43f44a01007ed2f149609f229636160d05c6fdc6c43bec591c95708b362f8aab021e54897d0000000000000000000000000000000000000000000000000000000000000000d380435af616021f6a03ba01a005d2f8b02fed2493c01d756ecd2547a3106960d1145b14b59f0000fd4005002eac3aeb063e9364529198a38eb18a0d7c5d3a630bde0cc12e1ffca7fbaa6377807958a9e92b76bc190613614a1320b99339bd9121b166b245631e5c0ac024e95ef94a2966d75548439b49af3e2a6e9b9947ba0036860fe8963461a473e004667a1cec98da3bd9c01e29673a52a0a3a742d423da536d67ed22b6d660901795949cb1e3b34f371df464d3787d4a0c4817e5f55574072a3a69729195085607f15db7730b659ae5af087d3e5ebeeb9de9ee998449c5d04ef29571d5ffd7195f090bee27d2e7bbc022866b6da0c594a1703caa104598c1ee6e98bff18a222976e3dd256443f4c9811113a5fd82abcde99fa981e2f8ed9cb725a01dd9b916f39ee740786d9bc503328d4241aaf67dd0dce768193c1c3fa9568ecf9f3701f771af7586dbeafcb8fe217f6798d54973c935fd49a10bf5d667954f3d631724c33e854be71ff799aca3b746b1be29cb80772ed20206d8861e150855d01528c511db41ba9ff27faf0f654824656da103e7d339b6aeb36dd2bdad3ab997f9093435ab7ade10c1fa05b36acdbc382ce3a44e148009ee9ad7f7d89f99d9fce2ae6d36ea6e3fea7d9fba07503aa84392c6b33afcd0f05d12e16e65ea18aa1249c0c57a19f63833b53b680bcb7761a616335afa3213c470deff58adbbaca2f38f11d5bae1c6089c52341eac7a638f566077addfe3d608fe20ca7400bbc26e09ca527b865c612f02a0240652a0f92e3c06b657600ccfcddd80542a76f252152d973f3a75e91a398f5111e54f77ae87ee23639ae556b5c774a5f3786fa14617dea668eb962e5b332b36b06f63dfe9c9c05746a30d6eba52d78bb43de63a75befe5f65d20beab5ba9524a93960561f22178a5146f24c62d3cad703bc0ae516fe9b8449d1907d479fe37e247e2527121e5998f53839fdd717dbd356feedd3c775fe29e631ebbaa880028af966a197ff9ad684216a607b8b91cf649a52a732fc633c28a420e3ee74657bd3aebba605fed60a6a0b8b9981e88ade1beda722231941023e260533b6b70e84c0b47f88800ad95aa184f9951e59706d1d3446052677a2c1cf293cba91816f0478032a1192ba73350d35c17ccbd5a00eeefc5bfccce5979f3d4c9b8cd107b3c88f45a66e49d62d40ad489f86bcadab888cfb0c76464471658db1b5b962c9552845c92f3cfbcba310340ad994196a5d97eec99a6bebb79a6a831a8c7632498ec747a3d81d34c4659af8b5ec797efdccd3b6187b0f65f0a97e33d3ba4250682df1612be4ef20243c5186dc1026b0fde9d0388a926b3552fad67e92b81bcb0e27b756c212fddd62a71ac5036e6125bb1b813fceab9730a62189bcdb44ba442f4e4670811469b21f44cbdda3888ac47b889633df6d44de6bf1979f9f49f13bb4fda92f2fd9ac84ce58b47fe1eba656f5cd02b7a78d7681752874fce1b656b79e030c6c59d254104446741cedc167958331c50121c9a1778bd436c203f63a146251ea77c503d74160be9b19f4c833164e071db3b9cb16071f7b4ce3684a71d8b909c48a831c05ddbf0ed28638447958d156429b8f10b819fccd4c275b77c7161751ecee6e9a4d7e7617c6d7b6bf92d516a1659b86e55a3bfd02326b7da22f928e88b6ab8d286fd27a7112f7810b33f65600e93ac20dc277843704eca1403d6233edc064d356b0b9a2e10e0679b7f31f8ca280e9ce5d7df2b0ecb161f0f59791353de3262a0d2d4f0122efa178dc561dcfb5b9322b6a9eb1c55c258e8c2858f2ed444a2892b0c804bb4d329f76b605c5d2b997428429d341329c6a79d492096a172b1b171d6aed43daaa40f64f540638684fca407b19ec8506e56a8aaa057a6bb6d4a4bb603aae53a6be3dd34709b515bb4982e19e8e93c4237d46c17d439e6baa6a0601000000010000000000000000000000000000000000000000000000000000000000000000ffffffff060394aa020106ffffffff0223d09a3b000000001976a914ac5881394afb09fd322d4357f62919033a40fbfa88ac80b2e60e0000000017a91497d9bef69a863d2c1a65058c45c56b5ff64722f7870000000001000000037c29a35e2cf2097e883d31883e90a676d9bfd95b36bc65c5f26a7a6ee0209341000000006a47304402207a9c2c00ec27f56ba2cd7a5f93ddb54099bc9306505bc9a5fad4fb0baa7e7e6902201bf35e8e8d3c4597ae276c70ed2fff7fd87947ffbc20ce1aa6960ab4c1516a1101210212d0e07e3ad380457f25528499d34649d0bf9df8b9c312d4a1d417a278e02bf8feffffffee2ec4dde5f501feb47b3126a2c85cdf5184f9f055ba607997da6ba0c10c31a2000000006a4730440220645d523c8f81efa6c67213431620935a0502d07dc6570b0c43de8e0b10c3486002207d2e3cf7372e7cc0f394d9ee7d6b3a6d5a8be7b12c8537ab6ca972c0994ad58101210212d0e07e3ad380457f25528499d34649d0bf9df8b9c312d4a1d417a278e02bf8feffffff775fe2d64ff56f9f27075b85c8976736c3291add3b022d632f3fb370255006c6010000006a473044022055959a07c9e3329a6b1eae4fe1a04ed820e7396fb544da7dd96622d091495e0302207818c702fd0443a6179c42ff8d30d6ce5b6795b5f8ee009f2ad14587794d9986012102c6c6be4c0d25a6428456ecbf48727300c81a8a44cf8d42123c3b71da9dcca90afeffffff02445b1a00000000001976a914f6111327674b0073631ea6bebb4d5324fe8d729588ac00a3e111000000001976a91403e105ea5dbb243cddada1ec31cebd92266cd22588ac89aa0200010000000134a29e5b5422a4671f5895e7a65ca810922faa489f842022edecdb1db67278bd000000006a4730440220295e52d9c1c27e39c39631b4a81707a76548c54367b87eccb56377488e6f237702207da205da6cedfc8957c032c3e8ca68076e88f3e294e0bcc5e3b09e220e913a090121023ac084ff290026ba16d803a9b27fb53526c12bae2dc7f02680c390bc4957fb22feffffff022b0e1b00000000001976a914f754ef3d439695a8db9cd1728e416f5bfd93c8af88ac204e0000000000001976a914ad2fff299c080d343e40792b0e586740c8e28a2688ac89aa02000100000001f77db6e1c701b5f2da1345c628128bf6dd59ce413934e554ee5f038084187f8f000000006a4730440220115be5a6c9eaddbf5f316cbd7921096b11e5c017ab041c330d42a8a49b8442240220711bc1aceba1836b3052fbb7d60b3872c9f2ed1d458d57e193740ea3ad8236bd01210228f662b0ea4c01e9fedbbb1317691e4f4c229099bc2c90e98dc0c7c3bb54c1ddfeffffff02204e0000000000001976a914ad2fff299c080d343e40792b0e586740c8e28a2688ac290e1b00000000001976a914bc399c1dbc8112f2d8380fe545aa8967709ed6bf88ac89aa020001000000014be8853b9787c32118bd5860458816376adff16a1ad9fea7e66e0966545f817c010000006a4730440220752d04428a42498731dcc67fec3e8ac398499fe12d402c85f371a722b0ee059602206ac2924485277811157b727b7d17e2775603ac40da7c07785e38231fb3be4f95012102b484f85578837f9ea217b98c4d56bcf5cf8b5e3f82a37fd144c2dc4ad85ded11feffffff02d0070000000000001976a914ad2fff299c080d343e40792b0e586740c8e28a2688ac23580f00000000001976a91482d743c859f92f608c1c11f7617ad98bc5f246dc88ac89aa020001000000024be8853b9787c32118bd5860458816376adff16a1ad9fea7e66e0966545f817c000000006a4730440220595c9a44ddd76df7ed51108f4de230a7f6b02f723a4e9a7cb876ed77d058d34d02200d60d3bcc48d80224ece42f9925653704e11ebb2e7ef3f1480371396643f061f012103ad281d7a499689ab70e9687af38d3b1bf7b1b4b989cd4f63c4b90a20eff015a0feffffffc0b48d45b2e83cbcee4dbc196203501c454ed15c3710540fe4892a0a279ff262010000006b483045022100f074cd76d791225bb8e094314d93021a311915acc0528ad5076256b719314f34022002ab41c11563fcc47ab4c29941f29eac02246cdcf9f509704b239d4a01d867710121034d1092e2aaf9f538ee1f0781ce584bfcaab840080e031ea6dc34a11f22d23288feffffff0246480f00000000001976a914c017c594a76a661eee457f04f1ca7f0f5081be1088acd0070000000000001976a914ad2fff299c080d343e40792b0e586740c8e28a2688ac89aa0200";
            var block = Block.Parse(rawBlock);

            var rawTx = "01000000037c29a35e2cf2097e883d31883e90a676d9bfd95b36bc65c5f26a7a6ee0209341000000006a47304402207a9c2c00ec27f56ba2cd7a5f93ddb54099bc9306505bc9a5fad4fb0baa7e7e6902201bf35e8e8d3c4597ae276c70ed2fff7fd87947ffbc20ce1aa6960ab4c1516a1101210212d0e07e3ad380457f25528499d34649d0bf9df8b9c312d4a1d417a278e02bf8feffffffee2ec4dde5f501feb47b3126a2c85cdf5184f9f055ba607997da6ba0c10c31a2000000006a4730440220645d523c8f81efa6c67213431620935a0502d07dc6570b0c43de8e0b10c3486002207d2e3cf7372e7cc0f394d9ee7d6b3a6d5a8be7b12c8537ab6ca972c0994ad58101210212d0e07e3ad380457f25528499d34649d0bf9df8b9c312d4a1d417a278e02bf8feffffff775fe2d64ff56f9f27075b85c8976736c3291add3b022d632f3fb370255006c6010000006a473044022055959a07c9e3329a6b1eae4fe1a04ed820e7396fb544da7dd96622d091495e0302207818c702fd0443a6179c42ff8d30d6ce5b6795b5f8ee009f2ad14587794d9986012102c6c6be4c0d25a6428456ecbf48727300c81a8a44cf8d42123c3b71da9dcca90afeffffff02445b1a00000000001976a914f6111327674b0073631ea6bebb4d5324fe8d729588ac00a3e111000000001976a91403e105ea5dbb243cddada1ec31cebd92266cd22588ac89aa0200";
            var tx = Transaction.Parse(rawTx);
             
            var mainnetRawShieldedTx = "0200000000024811a903000000001976a914b6f7b34439b65f509eeefecab786c290d4b18dd388ac10270000000000001976a91438fef011da24095f9fcaf2a8be83af8d478c753b88ac00000000010000000000000000685fa90300000000ec1c13cc2973a7d781b4eb6102cc78ec90601ab470e8f3b7ff8e9a257a3b43883a9859f84866e546f1cfae4080b054fa188f89e126ef3397ef496065d4dfa1c146b8f487ec18e7d6a5f1ab733c692f3782415826e207a0372883910d23457bccf6c9d4c8d67fe653a1ff4616e52095a3d12d973a4e679edbca690f5d1a6366a31a32cc27fb0ea20e27d7f9cd9db16fa4ad84b4faf71d27f2a3a6c29aa8e76a19c51859ae82263b4933ac776b26885013363c6cd95c3543729060b28172c8f51a487eaeaee1ee411f817d79f459568fe81d3bca726e39b62c0e799c9c679d87b2a56176070160b7aebfb17952a6c0b5a43e7e8cf206fbeeabe36e524bf5e369cdcb856d6911b63d8ac09f3d75964f63f879e93d962cddf80b9ee8f8fe93a9fdae0205389a5c5119b8ac1f6eb8bfbb0f8be46ad1a9facae63efc98b1c99a58e4c19a021b83aa2250a9529112a1e168de7b696c23ed9255012bd2aa4cd7b6c81b2481f80b07653a969516bf7f50fee44998a2380ca8b72a1309d8f05c92e15c3f7c3af95303a51c86ff3bde3ae15710164f7d4a74003c1ad6d617a13297ff891fe63fd1510328d9f5885a3b664f398fd7c1c7dce3e3d57db32c297704b6fd1aac7f404eac50031718e20f6092ee8dee988f991f854a7cb1b30defa9bb87978e6485472d191941022620d9a5c1c46f178cd0fe7c71b787c988cababc627983ad88ff8e6269bfe0f903077b99ef0061db7c0879c2b47ca013c3ad6d5626d60fafd0f4112ef3281174720303d5c6025d7988092a46f623e7c3e49de53f2ca33bee7135d6a53840a4c13adaba78bb01e66dcfa89fde8a983b2d1892c5c2120e1547302967c01d22fe9ad598306e7132ae10e66bb24285dc74e0cc80c7d1c4b8cd1f451a94c1a8fac2c6223fd722448035feef15a81db77b5d70d0923dc38de4a64d2f4d1a3af1d4cc5d8f270a20b4ddfce3410f47ab233434d4706130e53d84fc7bd00c8608afe3d691b826124d38d053a80412717589f9457397b8000865538af80f9d069b9647f107096676ff1ca26c2e5e37f611ba9626ae0f86c69f4a6bf322ca1451836c0938afea131c8eaf0c8fd639dfcf6291a43d77a6d6f108840a9911dda76210b79cd076d518933a525b56600ab91244e55fdbc19ed66a447834a6a948cdd438581a63be9f727837bd13c7ac6a27f518d80a7830c952e029b4a98b7e6057393a8278154e10df0bc523e88dff75be41890ab70cf3176f88fca558f2eaa7b4ca9c9f9a893f757c06023200a7b52a44f97ed2f15978b043190a818f56ff9add47bc7e67ce76f29f3ce5bda0f85849b6b483e626d2bbec6ebdfdfe2ab29574883d44d0cbf8d559c79542367494f1379d6c330065eee5e4717612f5d5f0f20cd2c16056f93fcc6eb0b637d8ecd776ab4032a3a00fe00da6772f55167e01754377e8f391b423aac34d997e3f1051ff6305584588186933bbc42e7d4c8f7df878a1be7ea359013c7e9aa4c01aa98ab3cd9a003f851c9964d9021de7e562dc0d766fc98e22591a181c3b1c05f8fc84f3817d4a2b4b022ad3038c7d0980eac0b1d1c618a6fa5a539f84ede35c1c9e6ad941fee26405046ab1cc84f0c266defcbfe22558c16b58c191647ab39edba906c39aabd20f6a9edb94189a36b8a6423761cf3d742018231166685198ec23f9eef89485d991574d6b56488a70a4a9bee01e4ce80f133455bf21a51bfe286626fca3070a82440f6ed1d6626af7447bbb5925323a7b5a3523bbdb667e4cbd6e6a1a9e524d830de92b0a2e86d8069b293be34921eb19a8039c90c913a2376e0df8452ace539d09d152e74f88622334073d8cdcb1aa1be7449e860bb5f193a325bd521d436726ad6072e2583a6a4f94a101c7391d0ddd3f8937b9a9203d97e355539a3955ebdef7b1e03edbcff56310cd83e3d94179917dea47c4da62b226c98c042f614a4ca86e0ddff2e341557fe330ceb496ea7bb64da4988125f4145dfea8f3ccdc5c2472a52b12b3db185df8b980bc7538a66fbf070d1d5639207dc88ce6b7891970cf814bb46abc3a82c87db180996a7374871a28e5e2398f3ac6b77236f6f830a34850b362a852f26ddffef32347f00b5eff3df3ea48d6fad807d6e88cf7ed704147ca3c9bf2ce3cd5f4a17c8fda1d1e2efc189e60beb6fa37c113a2ecb6a015213ad0e29cd4698754294eb5d3a9bd3aa0f16bcae54e2d94811a9ea12fa67190e2423a276f8820ea082b627226b23c0021c43ff00e45f541775571b11f2a8d5a9ee5af9c2245b9c8ea42630f007b33ad33835af32c86145f54b084d9968f0114a860a521fa35c3e1a572ce7c5e485541d68ef65b137f5cd6415d542d41d8d84804f2f0e68b83ddaeffd8596f81f7189c45087cfcf9d6522498ddcbfcdaacc96ed0fde453a7ac8a7181ae1c0150d94554c32b09477ca51bc3b650385844244f0a6f7a11b58644c821573659447cddb9c3d079a95964230b9bc0944bd86fcea7359121246ff2818f8f156f0a736fb36d0e28fa4e50821aed9076d9d05feb2725476a52bdda3f4a082bb2fd7c19016a543e72174e8cbc397b578fe7e841f6a6e24a2e76cdb8b5acd6d8218804150f77563240d7522a48aa8623083d0a36134dccd4e88b3a7c6bac5559fd74ae00";
            var shieldedTx = Transaction.Parse(mainnetRawShieldedTx);
        }
    }
}