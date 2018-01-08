using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Service.BlockchainApi.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract.Responses.PendingEvents;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Zcash.Api.Models;
using Lykke.Service.Zcash.Api.Models.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/transactions")]
    public class TransactionsController : Controller
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILog _log;
        private readonly ZcashApiSettings _settings;

        public TransactionsController(
            IBlockchainService blockchainService,
            ITransactionRepository transactionRepository,
            ILog log,
            ZcashApiSettings settings)
        {
            _blockchainService = blockchainService;
            _transactionRepository = transactionRepository;
            _log = log;
            _settings = settings;
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

            var txContext = string.Empty;
            var tx = await _transactionRepository.Get(request.OperationId);

            if (tx == null)
            {
                txContext = await _blockchainService.BuildTransactionAsync(from, to, amount, request.IncludeFee);

                tx = await _transactionRepository.Upsert(TransactionState.Built,
                    request.OperationId, request.FromAddress, request.ToAddress, request.AssetId, request.Amount,
                    context: txContext);

                await _log.WriteInfoAsync(nameof(Build),
                    $"Tx: {tx.ToJson()}, Context: {txContext}",
                    $"Transaction built");
            }
            else
            {
                txContext = tx.Context;

                await _log.WriteInfoAsync(nameof(Build),
                    $"Tx: {tx.ToJson()}, Context: {txContext}",
                    $"Transaction building re-requested");
            }

            return Ok(new BuildTransactionResponse
            {
                TransactionContext = txContext
            });
        }

        [HttpPut]
        [ProducesResponseType(typeof(BuildTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status405MethodNotAllowed)]
        public async Task<IActionResult> Rebuild([FromBody]RebuildTransactionRequest request)
        {
            if (!ModelState.IsValid ||
                !_blockchainService.IsValidRequest(ModelState, request, out var from, out var to, out var asset, out var amount))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            if (!_settings.EnableRbf)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed,
                    ErrorResponse.Create("Replace-By-Fee functionality is turned off"));
            }

            var txContext = await _blockchainService.BuildTransactionAsync(from, to, amount, request.IncludeFee, request.FeeFactor);

            var tx = await _transactionRepository.Upsert(TransactionState.Built,
                request.OperationId, request.FromAddress, request.ToAddress, request.AssetId, request.Amount, 
                context: txContext);

            await _log.WriteInfoAsync(nameof(Rebuild),
                    $"Tx: {tx.ToJson()}, Context: {txContext}",
                    $"Transaction re-built");

            return Ok(new BuildTransactionResponse
            {
                TransactionContext = txContext
            });
        }

        [HttpPost("broadcast")]
        [ProducesResponseType(typeof(BuildTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status424FailedDependency)]
        public async Task<IActionResult> Broadcast(BroadcastTransactionRequest request)
        {
            if (!ModelState.IsValid ||
                !_blockchainService.IsValidRequest(ModelState, request, out var transaction))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            var tx = await _transactionRepository.Get(request.OperationId);

            if (tx == null)
            {
                return StatusCode(StatusCodes.Status424FailedDependency,
                    ErrorResponse.Create("Transaction must be built with Zcash API to be successfully broadcasted"));
            }
            else if (tx.State == TransactionState.Sent && tx.Hex == request.SignedTransaction)
            {
                return StatusCode(StatusCodes.Status409Conflict, 
                    ErrorResponse.Create("Transaction already sent"));
            }        

            var txHash = await _blockchainService.BroadcastTransactionAsync(transaction);

            tx = await _transactionRepository.Upsert(TransactionState.Sent,
                tx.OperationId, tx.FromAddress, tx.ToAddress, tx.AssetId, tx.Amount, 
                hash: txHash, hex: request.SignedTransaction);

            await _log.WriteInfoAsync(nameof(Rebuild),
                $"Tx: {tx.ToJson()}",
                $"Transaction sent");

            return Ok();
        }
    }
}
