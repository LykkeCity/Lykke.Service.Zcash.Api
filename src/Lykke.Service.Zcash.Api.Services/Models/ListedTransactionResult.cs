using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class ListedTransactionResult
    {
        public ListedTransaction[] Transactions { get; set; }
        public string LastBlock { get; set; }
    }
}
