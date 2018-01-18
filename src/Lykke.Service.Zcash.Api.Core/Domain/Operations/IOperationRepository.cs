using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public interface IOperationRepository
    {
        Task<IOperationalTransaction> CreateAsync(Guid operationId, string fromAddress, string toAddress, string assetId, decimal amount, decimal? fee, string signContext);

        Task<IOperationalTransaction> UpdateAsync(Guid operationId,
            DateTime? sentUtc = null, DateTime? completedUtc = null, DateTime? failedUtc = null, DateTime? deletedUtc = null,
            string signedTransaction = null, string hash = null, string error = null);

        Task<IOperationalTransaction> GetAsync(Guid operationId);

        Task<Guid?> GetOperationIdAsync(string hash, string toAddress);
    }
}
