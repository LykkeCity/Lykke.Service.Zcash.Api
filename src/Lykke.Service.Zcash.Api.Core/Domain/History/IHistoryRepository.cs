using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;

namespace Lykke.Service.Zcash.Api.Core.Domain.History
{
    public interface IHistoryRepository
    {
        Task UpsertAsync(ObservationSubject subject, Guid operationId, string hash, DateTime timestampUtc, string fromAddress, string toAddress, decimal amount, string assetId);

        Task<IEnumerable<ITransaction>> GetByAddressAsync(ObservationSubject type, string address, string afterHash = null, int take = 100);
    }
}
