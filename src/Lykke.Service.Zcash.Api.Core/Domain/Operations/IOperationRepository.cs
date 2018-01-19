using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public interface IOperationRepository
    {
        Task<IOperation> CreateAsync(Guid operationId, string fromAddress, string toAddress, string assetId, decimal amount, decimal fee, string signContext);

        Task<IOperation> UpdateAsync(Guid operationId,
            DateTime? sentUtc = null, DateTime? completedUtc = null, DateTime? failedUtc = null, DateTime? deletedUtc = null,
            string signedTransaction = null, string hash = null, string error = null);

        Task<IOperation> GetAsync(Guid operationId, bool loadItems = true);

        Task<IOperation> GetAsync(string transactionHash, bool loadItems = true);
    }
}
