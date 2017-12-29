using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainSignService.Client;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models;
using Lykke.Service.Zcash.Api.Models.Wallets;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/wallets")]
    public class WalletsController : Controller
    {
        private readonly IBlockchainService _blockchainService;

        public WalletsController(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        /// <summary>
        /// Generates new t-address for Zcash
        /// </summary>
        /// <returns>Public address</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CreateTransparentWalletResponse), 200)]
        public async Task<IActionResult> CreateTransaprentWallet()
        {
            return Ok(new CreateTransparentWalletResponse(await _blockchainService.CreateTransparentWalletAsync()));
        }

        /// <summary>
        /// Sends funds from hot-wallet to user's address
        /// </summary>
        /// <param name="address">Hot-wallet address</param>
        /// <param name="request">Pay-out parameters</param>
        /// <returns>Operation identifier</returns>
        [HttpPost("{address}/cashout")]
        [ProducesResponseType(typeof(CashoutResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> Cashout(string address, [FromBody]CashoutRequest request)
        {
            if (!_blockchainService.IsValidAddress(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid cashout address"));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ErrorResponse.Create(ModelState));
            }

            var operationId = new Guid();// await _transactionService.Transfer(address, request.To, request.Money, request.Signers);

            return Ok(new CashoutResponse(operationId));
        }
    }
}
