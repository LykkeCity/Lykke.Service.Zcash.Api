using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/balances")]
    public class BalancesController : Controller
    {
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<WalletBalanceContract>), StatusCodes.Status200OK)]
        public IActionResult Get([FromQuery]int? skip, [FromQuery]int? take)
        {
            return null;
        }

        [HttpPost("{address}/observation")]
        public IActionResult Post(string adderss)
        {
            return null;
        }

        [HttpDelete("{address}/observation")]
        public IActionResult Delete(string adderss)
        {
            return NoContent();
        }
    }
}
