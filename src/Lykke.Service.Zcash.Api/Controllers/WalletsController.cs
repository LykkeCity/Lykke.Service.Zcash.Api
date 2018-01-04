using System.Linq;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Service.BlockchainApi.Contract.Requests;
using Lykke.Service.BlockchainApi.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract.Responses.PendingEvents;
using Lykke.Service.Zcash.Api.Core.Domain.Events;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/wallets")]
    public class WalletsController : Controller
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IPendingEventRepository _pendingEventRepository;

        public WalletsController(
            IBlockchainService blockchainService, 
            IPendingEventRepository pendingEventRepository)
        {
            _blockchainService = blockchainService;
            _pendingEventRepository = pendingEventRepository;
        }

        /// <summary>
        /// Generates new t-address for Zcash
        /// </summary>
        /// <returns>Public address</returns>
        [HttpPost]
        [ProducesResponseType(typeof(WalletCreationResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateTransaprentWallet()
        {
            return Ok(new WalletCreationResponse
            {
                Address = await _blockchainService.CreateTransparentWalletAsync()
            });
        }

        /// <summary>
        /// Sends funds from hot-wallet to user's t-address
        /// </summary>
        /// <param name="address">Hot-wallet address</param>
        /// <param name="request">Pay-out parameters</param>
        /// <returns>Operation status</returns>
        [HttpPost("{address}/cashout")]
        [ProducesResponseType(typeof(PendingEventsResponse<PendingCashinEventContract>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(PendingEventsResponse<PendingEventContract>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cashout(string address, [FromBody]CashoutFromWalletRequest request)
        {
            if (!_blockchainService.IsValidAddress(address, out var from))
            {
                return BadRequest(ErrorResponse.Create("Invalid cashout address"));
            }

            if (!_blockchainService.IsValidRequest(request, ModelState, out var to, out var asset, out var money, out var signers))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            var existentEvents = await _pendingEventRepository.Get(request.OperationId);
            if (existentEvents.Any())
            {
                return StatusCode(
                    StatusCodes.Status409Conflict,
                    new PendingEventsResponse<PendingEventContract>
                    {
                        Events = existentEvents
                            .Select(e => e.ToContract())
                            .ToList()
                            .AsReadOnly()
                    });
            }

            var hash = await _blockchainService.TransferAsync(from, to, money, signers);

            var pendingEvent = await _pendingEventRepository.Create(EventType.CashoutStarted, request.OperationId, address,
                request.AssetId, request.Amount, request.To, hash);

            return CreatedAtAction(
                nameof(PendingEventsController.Get), 
                nameof(PendingEventsController), 
                new
                {
                    operationId = pendingEvent.OperationId
                },
                new PendingEventsResponse<PendingCashinEventContract>
                {
                    Events = new PendingCashinEventContract[] { pendingEvent.ToCashin() }
                });
        }
    }
}
