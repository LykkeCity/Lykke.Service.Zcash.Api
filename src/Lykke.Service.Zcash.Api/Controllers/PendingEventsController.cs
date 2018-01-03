using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Events;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models;
using Lykke.Service.Zcash.Api.Models.PendingEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/pending-events")]
    public class PendingEventsController : Controller
    {
        private readonly IPendingEventRepository _pendingEventRepository;
        private readonly Dictionary<string, EventType> _eventTypes = new Dictionary<string, EventType>
        {
            ["cashin"] = EventType.CashIn,
            ["cashout-started"] = EventType.CashOutStarted,
            ["cashout-completed"] = EventType.CashOutCompleted,
            ["cashout-failes"] = EventType.CashOutFailed
        };

        public PendingEventsController(IPendingEventRepository pendingEventRepository)
        {
            _pendingEventRepository = pendingEventRepository;
        }

        [HttpGet("{operationId}")]
        [ProducesResponseType(typeof(PendingEventResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([FromRoute]Guid operationId)
        {
            return Ok(new PendingEventResponse(await _pendingEventRepository.Get(operationId)));
        }

        [HttpGet("{typeId}")]
        [ProducesResponseType(typeof(PendingEventResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([FromRoute]string typeId, [FromQuery]int? maxEventsNumber)
        {
            if (!_eventTypes.TryGetValue(typeId, out var eventType))
            {
                return NotFound();
            }

            var events = await _pendingEventRepository.Get(eventType, maxEventsNumber);

            return Ok(new PendingEventResponse(events));
        }

        [HttpDelete("{typeId}")]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete([FromRoute]string typeId, [FromBody]PendingEventDeleteRequest request)
        {
            if (!_eventTypes.TryGetValue(typeId, out var eventType))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ErrorResponse.Create(ModelState));
            }

            await _pendingEventRepository.Delete(eventType, request.OperationIds);

            return Ok();
        }
    }
}
