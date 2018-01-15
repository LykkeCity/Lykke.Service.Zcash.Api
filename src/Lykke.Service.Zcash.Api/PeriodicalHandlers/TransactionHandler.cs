using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Zcash.Api.Core.Services;

namespace Lykke.Service.Zcash.Api.PeriodicalHandlers
{
    public class TransactionHandler : TimerPeriod
    {
        private ILog _log;
        private IBlockchainService _blockchainService;

        public TransactionHandler(int trackingInterval, ILog log, IBlockchainService blockchainService) :
            base(nameof(TransactionHandler), trackingInterval, log)
        {
            _log = log;
            _blockchainService = blockchainService;
        }

        public override async Task Execute()
        {
            try
            {
                await _blockchainService.HandleHistoryAsync();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(TransactionHandler), nameof(Execute), null, ex);
            }
        }
    }
}
