using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> Build(Guid operationId, OperationType type, Asset asset, bool subtractFees, params (string from, string to, decimal amount)[] items)
        {
            var operation = await _blockchainService.GetOperationAsync(operationId, loadItems: false);

            if (operation != null && operation.State != OperationState.Built)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    ErrorResponse.Create($"Operation is already {Enum.GetName(typeof(OperationState), operation.State).ToLower()}"));
            }

            try
            {
                var signContext = await _blockchainService.BuildAsync(operationId, type, asset, subtractFees, items);

                return Ok(new BuildTransactionResponse
                {
                    TransactionContext = signContext.context.ToBase64()
                });
            }
            catch (BuildTransactionException ex)
            {
                return BadRequest(BlockchainErrorResponse.FromKnownError(ex.Error == BuildTransactionException.ErrorCode.Dust ? 
                    BlockchainErrorCode.AmountIsTooSmall : BlockchainErrorCode.NotEnoughtBalance));
            }
        }

        private async Task<IActionResult> Get<TResponse>(Guid operationId, Func<IOperation, TResponse> toResponse)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidOperationId(operationId))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            var operation = await _blockchainService.GetOperationAsync(operationId);
            if (operation != null && operation.State != OperationState.Built && operation.State != OperationState.Deleted)
                return Ok(toResponse(operation));
            else
                return NoContent();
        }

        private async Task<IActionResult> Observe(string address, HistoryAddressCategory category)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(_blockchainService, address))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            if (await _blockchainService.TryCreateHistoryAddressAsync(address, category))
                return Ok();
            else
                return StatusCode(StatusCodes.Status409Conflict);
        }

        private async Task<IActionResult> DeleteObservation(string address, HistoryAddressCategory category)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(_blockchainService, address))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            if (await _blockchainService.TryDeleteHistoryAddressAsync(address, category))
                return Ok();
            else
                return NoContent();
        }

        private async Task<IActionResult> GetHistory(string address, string afterHash, int take, HistoryAddressCategory category)
        {
            if (take <= 0)
            {
                ModelState.AddModelError(nameof(take), "Must be greater than zero");
            }

            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(_blockchainService, address))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            var txs = await _blockchainService.GetHistoryAsync(category, address, afterHash, take);

            return Ok(txs
                .Select(tx => tx.ToHistoricalContract())
                .ToArray());
        }

        [HttpPost("single")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BuildTransactionResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BlockchainErrorResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> Build([FromBody]BuildSingleTransactionRequest request)
        {
            if (!ModelState.IsValid || 
                !ModelState.IsValidRequest(request, _blockchainService, out var items, out var asset))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            return await Build(request.OperationId, OperationType.SingleFromSingleTo, asset, request.IncludeFee, items);
        }

        [HttpPost("single/receive")]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public IActionResult BuildSingleReceive([FromBody]BuildSingleReceiveTransactionRequest request)
        {
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        [HttpPost("many-inputs")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BuildTransactionResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BlockchainErrorResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> Build([FromBody]BuildTransactionWithManyInputsRequest request)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidRequest(request, _blockchainService, out var items, out var asset))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            return await Build(request.OperationId, OperationType.MultiFromSingleTo, asset, true, items);
        }

        [HttpPost("many-outputs")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BuildTransactionResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BlockchainErrorResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> Build([FromBody]BuildTransactionWithManyOutputsRequest request)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidRequest(request, _blockchainService, out var items, out var asset))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BlockchainErrorResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> Broadcast([FromBody]BroadcastTransactionRequest request)
        {
            if (!ModelState.IsValid || 
                !ModelState.IsValidRequest(request, _blockchainService))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            var operation = await _blockchainService.GetOperationAsync(request.OperationId, false);

            if (operation == null)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ErrorResponse.Create("Transaction must be built beforehand by Zcash API (to be successfully broadcasted then)"));
            }

            if (operation.State != OperationState.Built)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    ErrorResponse.Create($"Operation is already {Enum.GetName(typeof(OperationState), operation.State).ToLower()}"));
            }

            await _blockchainService.BroadcastAsync(operation.OperationId, request.SignedTransaction);
            
            return Ok();
        }

        [HttpGet("broadcast/single/{operationId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BroadcastedSingleTransactionResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> GetSingle([FromRoute]Guid operationId)
        {
            return await Get(operationId, op => op.ToSingleResponse());
        }

        [HttpGet("broadcast/many-inputs/{operationId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BroadcastedTransactionWithManyInputsResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> GetManyInputs([FromRoute]Guid operationId)
        {
            return await Get(operationId, op => op.ToManyInputsResponse());
        }

        [HttpGet("broadcast/many-outputs/{operationId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BroadcastedTransactionWithManyOutputsResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> GetManyOutputs([FromRoute]Guid operationId)
        {
            return await Get(operationId, op => op.ToManyOutputsResponse());
        }

        [HttpDelete("broadcast/{operationId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteBroadcasted([FromRoute]Guid operationId)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidOperationId(operationId))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            if (await _blockchainService.TryDeleteOperationAsync(operationId))
                return Ok();
            else
                return NoContent();
        }

        [HttpPost("history/from/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ObserveFrom(
            [FromRoute]string address)
        {
            return await Observe(address, HistoryAddressCategory.From);
        }

        [HttpPost("history/to/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ObserveTo(
            [FromRoute]string address)
        {
            return await Observe(address, HistoryAddressCategory.To);
        }

        [HttpDelete("history/from/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteObservationFrom(
            [FromRoute]string address)
        {
            return await DeleteObservation(address, HistoryAddressCategory.From);
        }

        [HttpDelete("history/to/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteObservationTo(
            [FromRoute]string address)
        {
            return await DeleteObservation(address, HistoryAddressCategory.To);
        }

        [HttpGet("history/from/{address}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HistoricalTransactionContract[]))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> GetHistoryFrom(
            [FromRoute]string address,
            [FromQuery]string afterHash,
            [FromQuery]int take)
        {
            return await GetHistory(address, afterHash, take, HistoryAddressCategory.From);
        }

        [HttpGet("history/to/{address}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HistoricalTransactionContract[]))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> GetHistoryTo(
            [FromRoute]string address,
            [FromQuery]string afterHash,
            [FromQuery]int take)
        {
            return await GetHistory(address, afterHash, take, HistoryAddressCategory.To);
        }
    }
}
