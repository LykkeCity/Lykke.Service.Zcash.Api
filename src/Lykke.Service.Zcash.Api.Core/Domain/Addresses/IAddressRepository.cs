﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Zcash.Api.Core.Domain.Addresses
{
    public interface IAddressRepository
    {
        Task<bool> CreateIfNotExistsAsync(ObservationSubject subject, string address);

        Task<bool> DeleteIfExistAsync(ObservationSubject subject, string address);

        Task<IAddress> GetAsync(ObservationSubject subject, string address);

        Task<(IEnumerable<IAddress> items, string continuation)> GetBySubjectAsync(ObservationSubject subject, string continuation = null, int take = 100);
    }
}
