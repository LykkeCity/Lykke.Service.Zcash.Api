using System;
using Common.Log;

namespace Lykke.Service.Zcash.Api.Client
{
    public class ZcashApiClient : IZcashApiClient, IDisposable
    {
        private readonly ILog _log;

        public ZcashApiClient(string serviceUrl, ILog log)
        {
            _log = log;
        }

        public void Dispose()
        {
            //if (_service == null)
            //    return;
            //_service.Dispose();
            //_service = null;
        }
    }
}
