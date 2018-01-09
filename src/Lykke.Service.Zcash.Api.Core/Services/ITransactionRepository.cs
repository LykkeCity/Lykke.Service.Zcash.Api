using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface ITransactionRepository
    {
        Task<ITransaction> BuildAsync(Guid operationId, string fromAddress, string toAddress, string assetId, string amount, string signContext = null);
        Task SendAsync(ITransaction tx, string hash);
        Task FailAsync(ITransaction tx, string errorMessage);
        Task CompleteAsync(ITransaction tx);
        Task Delete(IEnumerable<Guid> operationIds);
        Task<IReadOnlyList<ITransaction>> Get(TransactionState? state = null, int skip = 0, int take = 0);
        Task<ITransaction> Get(Guid operationId);
    }
}
