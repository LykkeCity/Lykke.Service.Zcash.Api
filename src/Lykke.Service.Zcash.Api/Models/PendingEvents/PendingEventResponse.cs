using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lykke.Service.Zcash.Api.Core.Domain.Events;

namespace Lykke.Service.Zcash.Api.Models.PendingEvents
{
    public class PendingEventResponse
    {
        public PendingEventResponse(IEnumerable<IPendingEvent> pendingEvents)
        {
            Events = pendingEvents
                .Select(e => new PendingEventModel(e))
                .ToArray();
        }

        public PendingEventResponse(params IPendingEvent[] pendingEvents) : this(pendingEvents.AsEnumerable())
        {
        }

        public PendingEventResponse(IEnumerable<PendingEventModel> pendingEventModels) : this(pendingEventModels.ToArray())
        {
        }

        public PendingEventResponse(params PendingEventModel[] pendingEventModels)
        {
            Events = pendingEventModels;
        }

        public PendingEventModel[] Events { get; }
    }
}
