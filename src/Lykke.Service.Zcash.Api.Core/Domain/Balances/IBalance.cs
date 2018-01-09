using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Domain.Balances
{
    public interface IBalance
    {
        string Address { get; }
        Asset Asset { get; }
        Money Balance { get; }
    }
}
