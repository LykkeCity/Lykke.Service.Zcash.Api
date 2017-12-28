using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Services;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Services
{
    public class TransactionService : ITransactionService
    {
        public async Task<Guid> Transfer(BitcoinAddress from, BitcoinAddress to, Money amount, params BitcoinAddress[] signers)
        {
            return Guid.NewGuid();
        }
    }
}
