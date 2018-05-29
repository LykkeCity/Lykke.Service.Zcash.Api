using System.Collections.Generic;
using Lykke.Service.Zcash.Api.Core.Domain;

namespace Lykke.Service.Zcash.Api.Core
{
    public static class Constants
    {
        public static readonly IReadOnlyDictionary<string, Asset> Assets = new Dictionary<string, Asset>
        {
            [Asset.Zec.Id] = Asset.Zec
        };

        public static readonly decimal DefaultFee = 0.00010000M;
    }
}
