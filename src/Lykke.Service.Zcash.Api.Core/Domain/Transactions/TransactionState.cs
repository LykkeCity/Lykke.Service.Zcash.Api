using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Transactions
{
    public enum TransactionState
    {
        Built = 0,
        Sent = 1,
        Completed = 2,
        Failed = 3
    }
}
