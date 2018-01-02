using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Insight;
using Lykke.Service.Zcash.Api.Core.Services;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Services
{
    public class InsightClient : IInsightClient
    {
        public Task Broadcast(Transaction tx)
        {
            throw new NotImplementedException();
        }

        public Utxo[] GetUtxo(BitcoinAddress address)
        {
            throw new NotImplementedException();
        }
    }
}
