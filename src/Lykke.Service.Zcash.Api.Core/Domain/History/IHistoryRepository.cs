using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;

namespace Lykke.Service.Zcash.Api.Core.Domain.History
{
    public interface IHistoryRepository
    {
        Task CreateAsync(Guid operationId, string hash, DateTime timestamp, string fromAddress, string toAddress, decimal amount, string assetId);

        Task<IEnumerable<ITransaction>> GetByAddressAsync(AddressMonitorType type, string address, string afterHash = null, int take = 100);
    }
}
