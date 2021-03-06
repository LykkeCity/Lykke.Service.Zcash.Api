﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Zcash.Api.Core.Domain.Addresses
{
    public interface IAddressRepository
    {
        Task<bool> CreateBalanceAddressIfNotExistsAsync(string address);

        Task<bool> DeleteBalanceAddressIfExistsAsync(string address);

        Task<bool> CreateHistoryAddressIfNotExistsAsync(string address, HistoryAddressCategory category);

        Task<bool> DeleteHistoryAddressIfExistsAsync(string address, HistoryAddressCategory category);

        Task<(string continuation, IEnumerable<string> items)> GetBalanceAddressesChunkAsync(string continuation = null, int take = 100);

        Task<(string continuation, IEnumerable<string> items)> GetHistoryAddressesChunkAsync(string continuation = null, int take = 100);

        Task<bool> IsBalanceAddressExistsAsync(string address);

        Task<bool> IsHistoryAddressExistsAsync(string address, HistoryAddressCategory category);
    }
}
