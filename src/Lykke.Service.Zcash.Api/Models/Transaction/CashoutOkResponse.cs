using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Models.Transaction
{
    public class CashoutOkResponse
    {
        public CashoutOkResponse(Guid operationId) => OperationId = operationId;

        public Guid OperationId { get; }
    }
}
