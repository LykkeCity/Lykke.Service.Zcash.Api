using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.Zcash.Api.Core.Domain.Insight;
using Lykke.Service.Zcash.Api.Core.Services;
using NBitcoin;
using Newtonsoft.Json;

namespace Lykke.Service.Zcash.Api.Services
{
    public class InsightClient : IInsightClient
    {
        private readonly ILog _log;
        private readonly HttpClient _http = new HttpClient();

        public InsightClient(ILog log, string insightUrl)
        {
            _log = log;
            _http.BaseAddress = new Uri(insightUrl);
        }

        public async Task<SendTransactionResult> SendTransactionAsync(Transaction tx)
        {
            return await PostAsync<SendTransactionResult>("tx/send", ("rawtx", tx.ToHex()));
        }

        public async Task<Utxo[]> GetUtxoAsync(params BitcoinAddress[] addresses)
        {
            return await PostAsync<Utxo[]>("addrs/utxo", 
                ("addrs", string.Join(",", addresses.Select(a => a.ToString()))));
        }

        public async Task<T> PostAsync<T>(string url, params (string k, string v)[] pairs)
        {
            var dict = pairs.ToDictionary(
                t => t.k, 
                t => t.v);

            var resp = await _http.PostAsync(url, new FormUrlEncodedContent(dict));

            try
            {
                resp.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(PostAsync), $"Url: {resp.RequestMessage.RequestUri}", ex);
                throw;
            }

            var json = await resp.Content.ReadAsStringAsync();

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(PostAsync), $"Url: {resp.RequestMessage.RequestUri}, Response: {json}", ex);
                throw;
            }
        }
    }
}
