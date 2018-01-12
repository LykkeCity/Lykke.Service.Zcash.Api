using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core
{
    public class PagedResult<T>
    {
        public PagedResult() { }

        public PagedResult(string continuation, IReadOnlyList<T> items) => (Continuation, Items) = (continuation, items);

        public string Continuation { get; set; }

        public IReadOnlyList<T> Items { get; set; }
    }
}
