using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/balances")]
    public class BalancesController : Controller
    {
        private readonly IBlockchainService _blockchainService;

        public BalancesController(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }
        
        [HttpGet]
        [ProducesResponseType(typeof(PaginationResponse<WalletBalanceContract>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get(
            [FromQuery]string continuation,
            [FromQuery]int take)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidContinuation(continuation))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            var result = await _blockchainService.GetBalancesAsync(continuation, take);

            return Ok(PaginationResponse.From(
                result.continuation,
                result.items.Select(b => b.ToWalletContract()).ToArray()));
        }

        [HttpPost("{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromRoute]string address)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(address))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            if (await _blockchainService.TryCreateObservableAddressAsync(ObservationCategory.Balance, address))
                return Ok();
            else
                return StatusCode(StatusCodes.Status409Conflict);
        }

        [HttpDelete("{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete([FromRoute]string address)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(address))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            if (await _blockchainService.TryDeleteObservableAddressAsync(ObservationCategory.Balance, address))
                return Ok();
            else
                return NoContent();
        }
    }
}
