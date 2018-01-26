﻿using System;
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
using Common;
using Lykke.Service.Zcash.Api.Core.Domain;

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

        [NonAction]
        public async Task<IActionResult> Build(Guid operationId, OperationType type, (BitcoinAddress from, BitcoinAddress to, Money amount)[] items, Asset asset, bool subtractFees)
        {
            var operation = await _blockchainService.GetOperationAsync(operationId);

            if (operation != null &&
                operation.State != OperationState.Built)
            {
                var state = Enum.GetName(typeof(OperationState), operation.State).ToLower();

                return StatusCode(StatusCodes.Status409Conflict, ErrorResponse.Create($"Operation is already {state}"));
            }

            if (operation == null)
            {
                try
                {
                    operation = await _blockchainService.BuildAsync(operationId, OperationType.SISO, items, asset, subtractFees);
                }
                catch (NotEnoughFundsException)
                {
                    return Ok(new BuildTransactionResponse
                    {
                        ErrorCode = TransactionExecutionError.NotEnoughtBalance
                    });
                }
            }

            return Ok(new BuildTransactionResponse
            {
                TransactionContext = operation.SignContext.ToBase64()
            });
        }

        [HttpPost("single")]
        [ProducesResponseType(typeof(BuildTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Build([FromBody]BuildSingleTransactionRequest request)
        {
            if (!ModelState.IsValid || 
                !_blockchainService.IsValidRequest(ModelState, request, out var items, out var asset))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            return await Build(request.OperationId, OperationType.SISO, items, asset, request.IncludeFee);
        }

        [HttpPost("many-inputs")]
        [ProducesResponseType(typeof(BuildTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Build([FromBody]BuildTransactionWithManyInputsRequest request)
        {
            if (!ModelState.IsValid ||
                !_blockchainService.IsValidRequest(ModelState, request, out var items, out var asset))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            return await Build(request.OperationId, OperationType.MISO, items, asset, true);
        }

        [HttpPost("many-outputs")]
        [ProducesResponseType(typeof(BuildTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Build([FromBody]BuildTransactionWithManyOutputsRequest request)
        {
            if (!ModelState.IsValid ||
                !_blockchainService.IsValidRequest(ModelState, request, out var items, out var asset))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            return await Build(request.OperationId, OperationType.SIMO, items, asset, false);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public IActionResult Rebuild([FromBody]RebuildTransactionRequest request)
        {
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        [HttpPost("broadcast")]
        [ProducesResponseType(typeof(BroadcastTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Broadcast([FromBody]BroadcastTransactionRequest request)
        {
            if (!ModelState.IsValid || 
                !_blockchainService.IsValidRequest(ModelState, request, out var transaction, out var coins))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            _blockchainService.EnsureSigned(transaction, coins);

            var operation = await _blockchainService.GetOperationAsync(request.OperationId);

            if (operation == null)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ErrorResponse.Create("Transaction must be built beforehand by Zcash API to be successfully broadcasted then"));
            }

            if (operation.State == OperationState.Sent && operation.SignedTransaction == transaction.ToHex())
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    ErrorResponse.Create("Transaction already sent earlier"));
            }

            await _blockchainService.BroadcastAsync(operation.OperationId, transaction);
            
            return Ok(new BroadcastTransactionResponse());
        }

        [HttpGet("broadcast/single/{operationId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<BroadcastedSingleTransactionResponse> GetSingle([FromRoute]Guid operationId)
        {
            return (await _blockchainService.GetOperationAsync(operationId))?.ToSingleResponse();
        }

        [HttpGet("broadcast/many-inputs/{operationId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<BroadcastedTransactionWithManyInputsResponse> GetManyInputs([FromRoute]Guid operationId)
        {
            return (await _blockchainService.GetOperationAsync(operationId))?.ToManyInputsResponse();
        }

        [HttpGet("broadcast/many-outputs/{operationId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<BroadcastedTransactionWithManyOutputsResponse> GetManyOutputs([FromRoute]Guid operationId)
        {
            return (await _blockchainService.GetOperationAsync(operationId))?.ToManyOutputsResponse();
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
