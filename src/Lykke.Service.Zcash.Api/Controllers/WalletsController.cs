using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainSignService.Client;
using Lykke.Service.Zcash.Api.Core.Domain.Events;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models;
using Lykke.Service.Zcash.Api.Models.Wallets;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;

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
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> Cashout(string address, [FromBody]CashoutRequest request)
        {
            if (!_blockchainService.IsValidAddress(address, out var from))
            {
                return BadRequest(ErrorResponse.Create("Invalid cashout address"));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ErrorResponse.Create(ModelState));
            }

            var hash = await _blockchainService.TransferAsync(from, 
                request.Destination, request.Money, request.SignerAddresses);

            await _pendingEventRepository.Create(EventType.CashOutStarted, request.OperationId, address,
                request.AssetId, request.Amount, request.To, hash);

            return Ok();
        }
    }
}
