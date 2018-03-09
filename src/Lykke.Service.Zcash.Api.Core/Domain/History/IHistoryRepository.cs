using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;

namespace Lykke.Service.Zcash.Api.Core.Domain.History
{
    public interface IHistoryRepository
    {
        Task UpsertAsync(HistoryAddressCategory category, string affectedAddress, DateTime timestampUtc, string hash,
            Guid? operationId, string fromAddress, string toAddress, decimal amount, string assetId);

        Task<IEnumerable<IHistoryItem>> GetByAddressAsync(HistoryAddressCategory category, string address, string afterHash = null, int take = 100);
    }
}
