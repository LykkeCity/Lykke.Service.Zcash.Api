﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Events;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IPendingEventRepository
    {
        Task Create(EventType eventType, Guid id, string fromAddress, string assetId, string amount, string toAddress, string transactionHash);
        Task Delete(EventType eventType, IEnumerable<Guid> operationIds);
        Task<IEnumerable<IPendingEvent>> Get(EventType eventType, int? limit = 10000);
    }
}