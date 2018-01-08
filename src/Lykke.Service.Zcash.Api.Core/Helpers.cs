using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.Zcash.Api.Core.Domain;
using NBitcoin;

namespace System
{
    public static class Helpers
    {
        public static Money ToZec(this decimal amount)
        {
            return Money.FromUnit(amount, Asset.Zec.Unit);
        }
    }
}
