using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class AddressInfo
    {
        public string Address { get; set; }
        public string ScriptPubKey { get; set; }
        public string PubKey { get; set; }
        public bool IsValid { get; set; }
        public bool IsMine { get; set; }
        public bool IsWatchOnly { get; set; }
        public bool IsScript { get; set; }
        public bool IsCompressed { get; set; }
    }
}
