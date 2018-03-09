using System;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Domain
{
    public class Asset
    {
        public Asset(string id, int decimalPlaces, MoneyUnit unit) => (Id, DecimalPlaces, Unit) = (id, decimalPlaces, unit);

        public string Id { get; }
        public int DecimalPlaces { get; }
        public MoneyUnit Unit { get; }

        // static instances (constants)

        public static Asset Zec { get; } = new Asset("ZEC", 8, MoneyUnit.BTC);
    }
}
