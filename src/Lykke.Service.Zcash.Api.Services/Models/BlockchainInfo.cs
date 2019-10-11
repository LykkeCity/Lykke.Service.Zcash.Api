using System;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class BlockchainInfo
    {
        public ConsensusInfo Consensus { get; set; }

        public class ConsensusInfo
        {
            public string NextBlock { get; set; }
        }
    }
}
