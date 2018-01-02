﻿using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Domain
{
    public class Asset
    {
        public Asset(string id, int decimalPlaces, MoneyUnit unit) => (Id, DecimalPlaces, Unit) = (id, decimalPlaces, unit);

        public string    Id            { get; }
        public int       DecimalPlaces { get; }
        public MoneyUnit Unit          { get; }

        // static instances (constants)

        public static Asset Zatoshi { get; } = new Asset("Zatoshi", 0, MoneyUnit.Satoshi);
        public static Asset Zec     { get; } = new Asset("ZEC", 8, MoneyUnit.BTC);
    }
}
