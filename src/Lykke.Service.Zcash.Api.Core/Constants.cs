using System.Collections.Generic;
using Lykke.Service.Zcash.Api.Core.Domain;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core
{
    public static class Constants
    {
        public static readonly IReadOnlyDictionary<string, Asset> Assets = new Dictionary<string, Asset>
        {
            [Asset.Zec.Id] = Asset.Zec
        };

        public static readonly Money DefaultFee = Money.Coins(0.00010000M);

        public const string ADDRESS_CATEGORY_RECEIVE = "receive";

        public const string ADDRESS_CATEGORY_SEND = "send";
    }
}
