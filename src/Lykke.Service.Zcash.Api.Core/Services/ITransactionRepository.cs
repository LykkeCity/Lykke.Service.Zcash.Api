using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface ITransactionRepository
    {
        Task<ITransaction> Upsert(TransactionState state, Guid operationId, string fromAddress, string toAddress, string assetId, string amount,
            string context = null, 
            string hash = null,
            string hex = null,
            string error = null);
        Task Delete(IEnumerable<Guid> operationIds);
        Task<IReadOnlyList<ITransaction>> Get(TransactionState? state = null, int skip = 0, int take = 0);
        Task<ITransaction> Get(Guid operationId);
    }
}
