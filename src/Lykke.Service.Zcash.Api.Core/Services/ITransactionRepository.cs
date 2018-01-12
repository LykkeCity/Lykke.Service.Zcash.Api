using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface ITransactionRepository
    {
        Task<ITransaction> CreateAsync(Guid operationId, string fromAddress, string toAddress, string assetId, decimal amount, decimal fee, string signContext);

        Task UpdateAsync(TransactionState state, Guid operationId, DateTime? sentUtc = null, DateTime? completedUtc = null, DateTime? failedUtc = null, 
            string signedTransaction = null, string hash = null, string error = null);

        Task DeleteAsync(IEnumerable<Guid> operationIds);

        Task<PagedResult<ITransaction>> GetAsync(TransactionState? state = null, string continuation = null, int take = 0);

        Task<ITransaction> GetAsync(Guid operationId);
    }
}
