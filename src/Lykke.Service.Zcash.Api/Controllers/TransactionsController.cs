using System;
using System.Text;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/transactions")]
    public class TransactionsController : Controller
    {
        private readonly IBlockchainService _blockchainService;

        public TransactionsController(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(BuildTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Build([FromBody]BuildTransactionRequest request)
        {
            if (!ModelState.IsValid || 
                !_blockchainService.IsValidRequest(ModelState, request, out var from, out var to, out var asset, out var amount))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            var operation = await _blockchainService.GetOperationAsync(request.OperationId);

            if (operation != null && 
                operation.State != OperationState.Built)
            {
                var state = Enum.GetName(typeof(OperationState), operation.State).ToLower();

                return StatusCode(StatusCodes.Status409Conflict,
                    ErrorResponse.Create($"Operation is already {state}"));
            }

            //if (operation == null)
            {
                try
                {
                    operation = await _blockchainService.BuildAsync(request.OperationId,
                        from, to, amount, asset, request.IncludeFee);
                }
                catch (NotEnoughFundsException ex)
                {
                    return StatusCode(StatusCodes.Status406NotAcceptable,
                        ErrorResponse.Create(ex.Message));
                }
            }

            return Ok(new BuildTransactionResponse
            {
                TransactionContext = Convert.ToBase64String(Encoding.UTF8.GetBytes(operation.SignContext))
            });
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public IActionResult Rebuild([FromBody]RebuildTransactionRequest request)
        {
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        [HttpPost("broadcast")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Broadcast([FromBody]BroadcastTransactionRequest request)
        {
            if (!ModelState.IsValid || !_blockchainService.IsValidRequest(ModelState, request, out var _))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            var op = await _blockchainService.GetOperationAsync(request.OperationId);

            if (op == null)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ErrorResponse.Create("Transaction must be built beforehand by Zcash API to be successfully broadcasted then"));
            }
            else if (op.State == OperationState.Sent && request.SignedTransaction == op.SignedTransaction)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    ErrorResponse.Create("Transaction already sent earlier"));
            }

            await _blockchainService.BroadcastAsync(op, request.SignedTransaction);
            
            return Ok();
        }

        [HttpGet("broadcast/{operationId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<BroadcastedTransactionResponse> GetBroadcasted([FromRoute]Guid operationId)
        {
            return (await _blockchainService.GetOperationAsync(operationId))?.ToBroadcastedResponse();
        }

        [HttpDelete("broadcast/{operationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteBroadcasted([FromRoute]Guid operationId)
        {
            if (await _blockchainService.TryDeleteOperationAsync(operationId))
                return Ok();
            else
                return NoContent();
        }
    }
}
