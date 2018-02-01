using System.Linq;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.History;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/history")]
    public class HistoryController : Controller
    {
        private readonly IBlockchainService _blockchainService;

        public HistoryController(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        [HttpPost("{category}/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Observe(
            [FromRoute]HistoryObservationCategory category,
            [FromRoute]string address)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(address))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            if (await _blockchainService.TryCreateObservableAddressAsync((ObservationCategory)category, address))
                return Ok();
            else
                return StatusCode(StatusCodes.Status409Conflict);
        }

        [HttpGet("{category}/{address}")]
        [ProducesResponseType(typeof(HistoricalTransactionContract[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get(
            [FromRoute]HistoryObservationCategory category,
            [FromRoute]string address, 
            [FromQuery]string afterHash,
            [FromQuery]int take)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(address))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            var txs = await _blockchainService.GetHistoryAsync((ObservationCategory)category, address, afterHash, take);

            return Ok(txs
                .Select(tx => tx.ToHistoricalContract())
                .ToArray());
        }
    }
}
