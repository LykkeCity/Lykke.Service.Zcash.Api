using System;
using System.Collections.Generic;
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

        public async Task<SendResult> Send(Transaction tx)
        {
            var broadcastUrl = $"tx/send";

            var broadcastParams = new Dictionary<string, string>
            {
                ["rawtx"] = tx.ToHex()
            };

            var resp = await _http.PostAsync(broadcastUrl, new FormUrlEncodedContent(broadcastParams));

            return await Read<SendResult>(resp);
        }

        public async Task<Utxo[]> GetUtxo(BitcoinAddress address)
        {
            return await Read<Utxo[]>(await _http.GetAsync($"addr/{address.ToString()}/utxo"));
        }

        public async Task<T> Read<T>(HttpResponseMessage resp)
        {
            try
            {
                resp.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(Read), $"Url: {resp.RequestMessage.RequestUri}", ex);
                throw;
            }

            var json = await resp.Content.ReadAsStringAsync();

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(Read), $"Url: {resp.RequestMessage.RequestUri}, Response: {json}", ex);
                throw;
            }
        }
    }
}
