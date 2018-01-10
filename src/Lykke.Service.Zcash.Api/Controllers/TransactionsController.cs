using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core.Domain.Transactions;
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
        private readonly ILog _log;
        private readonly ZcashApiSettings _settings;

        public TransactionsController(
            IBlockchainService blockchainService,
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

            if (amount <= Money.Coins(_settings.MinFee))
            {
                return StatusCode(StatusCodes.Status406NotAcceptable, ErrorResponse.Create($"{amount} is less than minimal fee"));
            }

            var tx =
                (await _blockchainService.GetObservableTxAsync(request.OperationId)) ??
                (await _blockchainService.BuildUnsignedTxAsync(request.OperationId, from, to, amount, asset, request.IncludeFee));

            return Ok(new BuildTransactionResponse
            {
                TransactionContext = tx.SignContext
            });
        }

        [HttpPost("broadcast")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Broadcast([FromBody]BroadcastTransactionRequest request)
        {
            if (!ModelState.IsValid || !_blockchainService.IsValidRequest(ModelState, request))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            var tx = await _blockchainService.GetObservableTxAsync(request.OperationId);

            if (tx == null)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ErrorResponse.Create("Transaction must be built beforehand by Zcash API to be successfully broadcasted then"));
            }
            else if (tx.State == TransactionState.Sent && tx.SignedTransaction == request.SignedTransaction)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    ErrorResponse.Create("Transaction already sent earlier"));
            }

            await _blockchainService.BroadcastTxAsync(tx, request.SignedTransaction);
            
            return Ok();
        }

        [HttpGet("in-progress")]
        public async Task<InProgressTransactionContract[]> GetSent(
            [FromQuery]int skip = 0, 
            [FromQuery]int take = 100)
        {
            var transactions = await _blockchainService.GetObservableTxsByStateAsync(TransactionState.Sent, skip, take);

            return transactions
                .Select(tx => tx.ToInProgressContract())
                .ToArray();
        }


        [HttpGet("completed")]
        public async Task<CompletedTransactionContract[]> GetCompleted(
            [FromQuery]int skip = 0, 
            [FromQuery]int take = 100)
        {
            var transactions = await _blockchainService. GetObservableTxsByStateAsync(TransactionState.Completed, skip, take);

            return transactions
                .Select(tx => tx.ToCompletedContract())
                .ToArray();
        }


        [HttpGet("failed")]
        public async Task<FailedTransactionContract[]> GetFailed(
            [FromQuery]int skip = 0, 
            [FromQuery]int take = 100)
        {
            var transactions = await _blockchainService.GetObservableTxsByStateAsync(TransactionState.Failed, skip, take);

            return transactions
                .Select(tx => tx.ToFailedContract())
                .ToArray();
        }

        [HttpDelete]
        public async Task Delete([FromBody]Guid[] operationIds)
        {
            await _blockchainService.DeleteObservableTxsAsync(operationIds);
        }
    }
}
