using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;

namespace Lykke.Service.Zcash.Api.Core.Repositories
{
    public interface IOperationRepository
    {
        Task<IOperationalTransaction> CreateAsync(Guid operationId, string fromAddress, string toAddress, string assetId, decimal amount, decimal fee, string signContext);

        Task<IOperationalTransaction> UpdateAsync(Guid operationId, DateTime? sentUtc = null, DateTime? completedUtc = null, DateTime? failedUtc = null,
            string signedTransaction = null, string hash = null, string error = null);

        Task<IOperationalTransaction> GetAsync(Guid operationId);

        Task<IOperationalTransaction[]> GetByStateAsync(TransactionState state);

        Task<bool> DeleteIfExistAsync(Guid operationId);
    }
}
