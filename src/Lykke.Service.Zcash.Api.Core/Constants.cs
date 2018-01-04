using System.Collections.Generic;
using Lykke.Service.Zcash.Api.Core.Domain;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core
{
    public static class Constants
    {
        public static readonly IReadOnlyDictionary<string, Asset> Assets = new Dictionary<string, Asset>
        {
            [Asset.Zatoshi.Id] = Asset.Zatoshi,
            [Asset.Zec.Id] = Asset.Zec
        };

        public static readonly Money DefaultFee = Money.Coins(0.0001M);
    }
}
