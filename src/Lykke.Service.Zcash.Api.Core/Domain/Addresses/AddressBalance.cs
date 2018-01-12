using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Domain.Addresses
{
    public class AddressBalance
    {
        public BitcoinAddress Address { get; set; }
        public Asset Asset { get; set; }
        public Money Balance { get; set; }
    }
}
