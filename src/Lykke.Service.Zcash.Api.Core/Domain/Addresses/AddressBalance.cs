using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Domain.Addresses
{
    public class AddressBalance
    {
        public AddressBalance(string address, string assetId, decimal balance) => (Address, AssetId, Balance) = (address, assetId, balance);

        public string Address { get; }
        public string AssetId { get; }
        public decimal Balance { get; }
    }
}
