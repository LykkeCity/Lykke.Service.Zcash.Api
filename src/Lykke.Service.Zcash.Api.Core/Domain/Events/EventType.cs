using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Events
{
    public enum EventType
    {
        CashIn = 1,
        CashOutStarted = 2,
        CashOutCompleted = 3,
        CashOutFailed = 4
    }
}
