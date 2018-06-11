using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public interface IOperationRepository
    {
        Task<IOperation> UpsertAsync(Guid operationId, OperationType type, (string fromAddress, string toAddress, decimal amount)[] items,
            decimal fee, bool subtractFee, string assetId, uint expiryHeight);

        Task<IOperation> UpdateAsync(Guid operationId,
            DateTime? sentUtc = null, DateTime? minedUtc = null, DateTime? completedUtc = null, DateTime? failedUtc = null, DateTime? deletedUtc = null,
            string hash = null, string error = null, uint? block = null);

        Task<IOperation> GetAsync(Guid operationId, bool loadItems = true);

        Task<IOperation> GetAsync(string hash, bool loadItems = true);

        Task<IEnumerable<Guid>> GetExpiredAsync(uint expiryHeight);
    }
}
