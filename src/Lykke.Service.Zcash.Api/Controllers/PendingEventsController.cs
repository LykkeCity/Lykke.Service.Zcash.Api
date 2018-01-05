using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Contract.Requests;
using Lykke.Service.BlockchainApi.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract.Responses.PendingEvents;
using Lykke.Service.Zcash.Api.Core.Domain.Events;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/pending-events")]
    public class PendingEventsController : Controller
    {
        private readonly IPendingEventRepository _pendingEventRepository;

        public PendingEventsController(IPendingEventRepository pendingEventRepository)
        {
            _pendingEventRepository = pendingEventRepository;
        }

        [HttpGet("{operationId}")]
        public async Task<PendingEventsResponse<PendingEventContract>> Get([FromRoute]Guid operationId)
        {
            var events = await _pendingEventRepository.Get(operationId);

            return new PendingEventsResponse<PendingEventContract>
            {
                Events = events
                    .Select(e => e.ToContract())
                    .ToList()
                    .AsReadOnly()
            };
        }

        [HttpGet("cashin")]
        public async Task<PendingEventsResponse<PendingCashinEventContract>> GetCashin([FromQuery]int? maxEventsNumber)
        {
            var events = await _pendingEventRepository.Get(EventType.Cashin, maxEventsNumber);

            return new PendingEventsResponse<PendingCashinEventContract>
            {
                Events = events
                    .Select(e => e.ToCashin())
                    .ToList()
                    .AsReadOnly()
            };
        }

        [HttpGet("cashout-completed")]
        public async Task<PendingEventsResponse<PendingCashoutCompletedEventContract>> GetCashoutCompleted([FromQuery]int? maxEventsNumber)
        {
            var events = await _pendingEventRepository.Get(EventType.CashoutCompleted, maxEventsNumber);

            return new PendingEventsResponse<PendingCashoutCompletedEventContract>
            {
                Events = events
                    .Select(e => e.ToCashoutCompleted())
                    .ToList()
                    .AsReadOnly()
            };
        }

        [HttpGet("cashout-failed")]
        public async Task<PendingEventsResponse<PendingCashoutFailedEventContract>> GetCashoutFailed([FromQuery]int? maxEventsNumber)
        {
            var events = await _pendingEventRepository.Get(EventType.CashoutFailed, maxEventsNumber);

            return new PendingEventsResponse<PendingCashoutFailedEventContract>
            {
                Events = events
                    .Select(e => e.ToCashoutFailed())
                    .ToList()
                    .AsReadOnly()
            };
        }

        [HttpGet("cashout-started")]
        public async Task<PendingEventsResponse<PendingCashoutStartedEventContract>> GetCashoutStarted([FromQuery]int? maxEventsNumber)
        {
            var events = await _pendingEventRepository.Get(EventType.CashoutStarted, maxEventsNumber);

            return new PendingEventsResponse<PendingCashoutStartedEventContract>
            {
                Events = events
                    .Select(e => e.ToCashoutStarted())
                    .ToList()
                    .AsReadOnly()
            };
        }

        [HttpDelete("cashin")]
        public async Task DeleteCashin([FromBody]RemovePendingEventsRequest request)
        {
            await _pendingEventRepository.Delete(EventType.Cashin, request.OperationIds);
        }

        [HttpDelete("cashout-completed")]
        public async Task DeleteCashoutCompleted([FromBody]RemovePendingEventsRequest request)
        {
            await _pendingEventRepository.Delete(EventType.CashoutCompleted, request.OperationIds);
        }

        [HttpDelete("cashout-failed")]
        public async Task DeleteCashoutFailed([FromBody]RemovePendingEventsRequest request)
        {
            await _pendingEventRepository.Delete(EventType.CashoutFailed, request.OperationIds);
        }

        [HttpDelete("cashout-started")]
        public async Task DeleteCashoutStarted([FromBody]RemovePendingEventsRequest request)
        {
            await _pendingEventRepository.Delete(EventType.CashoutStarted, request.OperationIds);
        }
    }
}
