using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.Zcash.Api.Core.Domain;

namespace Lykke.Service.Zcash.Api.Core
{
    public static class Constants
    {
        public static IReadOnlyDictionary<string, Asset> Assets { get; } = new Dictionary<string, Asset>
        {
            [Asset.Zatoshi.Id] = Asset.Zatoshi,
            [Asset.Zec.Id] = Asset.Zec
        };
    }
}
