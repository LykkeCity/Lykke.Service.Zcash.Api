using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models;
using Lykke.Service.Zcash.Api.Models.Transaction;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    public class TransactionController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly IWalletService _walletService;

        public TransactionController(ITransactionService transactionService, IWalletService walletService)
        {
            _transactionService = transactionService;
            _walletService = walletService;
        }

        /// <summary>
        /// Sends funds from hot-wallet to user's address
        /// </summary>
        /// <param name="address">Hot-wallet address</param>
        /// <param name="request">Pay-out parameters</param>
        /// <returns>Operation identifier</returns>
        [HttpPost("api/wallets/{address}/cashout")]
        [ProducesResponseType(typeof(CashoutOkResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> Cashout(string address, [FromBody]CashoutRequest request)
        {
            if (!_walletService.IsValid(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid cashout address"));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ErrorResponse.Create(ModelState));
            }

            var operationId = await _transactionService.Transfer(address, request.To, request.Money, request.Signers);

            return Ok(new CashoutOkResponse(operationId));
        }

    }
}
