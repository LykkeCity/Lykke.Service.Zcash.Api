using System;
using Lykke.Service.Zcash.Api.Core.Domain;

namespace NBitcoin
{
    public static class MoneyExtensions
    {
        public static string ToRoundTrip(this Money self, Asset asset)
        {
            var value = self.ToUnit(asset.Unit) * (decimal)Math.Pow(10, asset.DecimalPlaces);

            return value.ToString("F0");
        }
    }
}
