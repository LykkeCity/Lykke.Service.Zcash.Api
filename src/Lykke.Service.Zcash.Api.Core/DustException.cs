using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core
{
    public class DustException : Exception
    {
        public DustException(string message, Money amount, BitcoinAddress address) : base($"{message}. Output: {address}:{amount}")
        {
        }
    }
}
