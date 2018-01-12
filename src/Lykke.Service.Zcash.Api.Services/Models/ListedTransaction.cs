﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class ListedTransaction
    {
        public string Address { get; set; }
        public decimal Amount { get; set; }
        public decimal? Fee { get; set; }
        public string TxId { get; set; }
        public long BlockTime { get; set; }
        public long Confirmations { get; set; }
        public string Category { get; set; }
    }
}
