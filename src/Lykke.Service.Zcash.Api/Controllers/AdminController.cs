using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/admin")]
    public class AdminController : Controller
    {
        private readonly IBlockchainService _blockchainService;

        public AdminController(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        [HttpPost("addresses/import")]
        public async Task<IActionResult> Import()
        {
            await _blockchainService.ImportAllObservableAddressesAsync();

            return Content("All observable addresses has been re-imported to node. It's highly recommended to restart node with -reindex option.");
        }
    }
}
