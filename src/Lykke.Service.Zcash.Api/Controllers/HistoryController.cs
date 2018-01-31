using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.History;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Observe(
            [FromRoute]HistoryObservationCategory category,
            [FromRoute]string address)
        {
            if (await _blockchainService.TryCreateObservableAddressAsync((ObservationCategory)category, address))
                return Ok();
            else
                return StatusCode(StatusCodes.Status409Conflict);
        }

        [HttpGet("{category}/{address}")]
        public async Task<HistoricalTransactionContract[]> Get(
            [FromRoute]HistoryObservationCategory category,
            [FromRoute]string address, 
            [FromQuery]string afterHash = null,
            [FromQuery]int? take = 100)
        {
            var txs = await _blockchainService.GetHistoryAsync((ObservationCategory)category, address, afterHash, take.Value);

            return txs
                .Select(tx => tx.ToHistoricalContract())
                .ToArray();
        }
    }
}
