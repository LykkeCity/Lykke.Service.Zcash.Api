using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public enum OperationType
    {
        SingleFromSingleTo,
        SingleFromMultiTo,
        MultiFromSingleTo,
        MultiFromMultiTo
    }
}
