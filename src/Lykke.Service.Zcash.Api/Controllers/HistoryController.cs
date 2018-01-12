using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/history")]
    public class HistoryController : Controller
    {
        private readonly IBlockchainService _blockchainService;

        [HttpPost("{type}/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ObserveFrom(
            [FromRoute]AddressMonitorType type,
            [FromRoute]string address)
        {
            if (await _blockchainService.TryCreateObservableAddressAsync(type, address))
                return Ok();
            else
                return StatusCode(StatusCodes.Status409Conflict);
        }

        [HttpGet("{type}/{address}")]
        public async Task<HistoricalTransactionContract[]> Get(
            [FromRoute]AddressMonitorType type,
            [FromRoute]string address, 
            [FromQuery]string afterHash = null,
            [FromQuery]int take = 100)
        {
            var txs = await _blockchainService.GetHistoryAsync(type, address, afterHash, take);

            return txs
                .Select(tx => tx.ToHistoricalContract())
                .ToArray();
        }
    }
}
