using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Addresses
{
    public interface IAddress
    {
        ObservationCategory ObservationSubject { get; }
        string Address { get; }
    }
}
