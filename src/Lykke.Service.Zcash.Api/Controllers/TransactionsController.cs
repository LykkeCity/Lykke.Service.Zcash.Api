using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.History;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

        [NonAction]
        public async Task<IActionResult> Build(Guid operationId, OperationType type, Asset asset, bool subtractFees, params (BitcoinAddress from, BitcoinAddress to, Money amount)[] items)
        {
            var operation = await _blockchainService.GetOperationAsync(operationId, loadItems: false);

            if (operation != null && operation.State != OperationState.Built)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    ErrorResponse.Create($"Operation is already {Enum.GetName(typeof(OperationState), operation.State).ToLower()}"));
            }

            var signContext = string.Empty;

            try
            {
                signContext = await _blockchainService.BuildAsync(operationId, OperationType.SingleFromSingleTo, asset, subtractFees, items);
            }
            catch (NotEnoughFundsException)
            {
                return Ok(new BuildTransactionResponse
                {
                    ErrorCode = TransactionExecutionError.NotEnoughtBalance
                });
            }

            return Ok(new BuildTransactionResponse
            {
                TransactionContext = signContext.ToBase64()
            });
        }

        [NonAction]
        public async Task<IActionResult> Get<TResponse>(Guid operationId, Func<IOperation, TResponse> toResponse)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidOperationId(operationId))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            var operation = await _blockchainService.GetOperationAsync(operationId);
            if (operation != null)
                return Ok(toResponse(operation));
            else
                return NoContent();
        }

        [HttpPost("single")]
        [ProducesResponseType(typeof(BuildTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Build([FromBody]BuildSingleTransactionRequest request)
        {
            if (!ModelState.IsValid || 
                !ModelState.IsValidRequest(request, out var items, out var asset))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            return await Build(request.OperationId, OperationType.SingleFromSingleTo, asset, request.IncludeFee, items);
        }

        [HttpPost("many-inputs")]
        [ProducesResponseType(typeof(BuildTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Build([FromBody]BuildTransactionWithManyInputsRequest request)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidRequest(request, out var items, out var asset))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            return await Build(request.OperationId, OperationType.MultiFromSingleTo, asset, true, items);
        }

        [HttpPost("many-outputs")]
        [ProducesResponseType(typeof(BuildTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Build([FromBody]BuildTransactionWithManyOutputsRequest request)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidRequest(request, out var items, out var asset))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            return await Build(request.OperationId, OperationType.SingleFromMultiTo, asset, false, items);
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
                !ModelState.IsValidRequest(request, out var transaction, out var coins))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            _blockchainService.EnsureSigned(transaction, coins);

            var operation = await _blockchainService.GetOperationAsync(request.OperationId, false);

            if (operation == null)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ErrorResponse.Create("Transaction must be built beforehand by Zcash API (to be successfully broadcasted then)"));
            }

            if (operation != null && operation.State != OperationState.Built)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    ErrorResponse.Create($"Operation is already {Enum.GetName(typeof(OperationState), operation.State).ToLower()}"));
            }

            await _blockchainService.BroadcastAsync(operation.OperationId, transaction);
            
            return Ok(new BroadcastTransactionResponse());
        }

        [HttpGet("broadcast/single/{operationId:guid}")]
        [ProducesResponseType(typeof(BroadcastedSingleTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSingle([FromRoute]Guid operationId)
        {
            return await Get(operationId, op => op.ToSingleResponse());
        }

        [HttpGet("broadcast/many-inputs/{operationId:guid}")]
        [ProducesResponseType(typeof(BroadcastedTransactionWithManyInputsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetManyInputs([FromRoute]Guid operationId)
        {
            return await Get(operationId, op => op.ToManyInputsResponse());
        }

        [HttpGet("broadcast/many-outputs/{operationId:guid}")]
        [ProducesResponseType(typeof(BroadcastedTransactionWithManyOutputsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetManyOutputs([FromRoute]Guid operationId)
        {
            return await Get(operationId, op => op.ToManyOutputsResponse());
        }

        [HttpDelete("broadcast/{operationId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteBroadcasted([FromRoute]Guid operationId)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidOperationId(operationId))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            if (await _blockchainService.TryDeleteOperationAsync(operationId))
                return Ok();
            else
                return NoContent();
        }

        [HttpPost("history/{category}/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
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

        [HttpDelete("history/{category}/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteObservation(
            [FromRoute]HistoryObservationCategory category,
            [FromRoute]string address)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(address))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            if (await _blockchainService.TryDeleteObservableAddressAsync((ObservationCategory)category, address))
                return Ok();
            else
                return NoContent();
        }

        [HttpGet("history/{category}/{address}")]
        [ProducesResponseType(typeof(HistoricalTransactionContract[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHistory(
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
