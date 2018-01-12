using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Addresses
{
    public interface IAddress
    {
        AddressMonitorType MonitorType { get; }
        string Address { get; }
    }
}
