using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Insight;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IInsightClient
    {
        Task<Utxo[]> GetUtxo(BitcoinAddress address);
        Task<SendResponse> Send(Transaction tx);
    }
}
