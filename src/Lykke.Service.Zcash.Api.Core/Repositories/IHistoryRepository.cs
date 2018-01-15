using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;

namespace Lykke.Service.Zcash.Api.Core.Repositories
{
    public interface IHistoryRepository
    {
        Task<ITransaction[]> GetAsync(AddressMonitorType type, string address, string afterHash = null, int take = 100);
        Task CreateAsync(Guid operationId, string hash, DateTime timestamp, string fromAddress, string toAddress, decimal amount, decimal? fee, string assetId);
    }
}
