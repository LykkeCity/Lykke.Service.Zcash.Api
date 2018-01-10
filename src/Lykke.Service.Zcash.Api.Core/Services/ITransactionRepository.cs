using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface ITransactionRepository
    {
        Task<ITransaction> CreateAsync(Guid operationId, string fromAddress, string toAddress, string assetId, string amount, string fee, string signContext);
        Task UpdateAsync(Guid operationId, DateTime? sentUtc = null, DateTime? completedUtc = null, DateTime? failedUtc = null, 
            string signedTransaction = null, string hash = null, string error = null);
        Task DeleteAsync(IEnumerable<Guid> operationIds);
        Task<IReadOnlyList<ITransaction>> Get(TransactionState? state = null, int skip = 0, int take = 0);
        Task<ITransaction> Get(Guid operationId);
    }
}
