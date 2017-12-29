using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain
{
    public class Asset
    {
        public Asset(string id, int decimalPlaces) => (Id, DecimalPlaces) = (id, decimalPlaces);

        public string Id            { get; }
        public int    DecimalPlaces { get; }

        // static instances (constants)

        public static Asset Zatoshi { get; } = new Asset("Zatoshi", 0);
        public static Asset Zec     { get; } = new Asset("ZEC", 8);
    }
}
