using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Models.Wallets
{
    public class CashoutResponse
    {
        public CashoutResponse(Guid operationId) => OperationId = operationId;

        public Guid OperationId { get; }
    }
}
