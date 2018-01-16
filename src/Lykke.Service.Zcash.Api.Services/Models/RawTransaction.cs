using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class RawTransaction
    {
        public string TxId { get; set; }
        public Input[] Vin { get; set; }
        public Output[] Vout { get; set; }

        public class Input
        {
            public string TxId { get; set; }
            public uint Vout { get; set; }
        }

        public class Output
        {
            public uint N { get; set; }
            public ScriptPubKey ScriptPubKey { get; set; }
        }

        public class ScriptPubKey
        {
            public string[] Addresses { get; set; }
        }
    }
}
