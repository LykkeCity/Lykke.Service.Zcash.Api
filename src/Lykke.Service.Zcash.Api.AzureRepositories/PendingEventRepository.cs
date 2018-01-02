﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Lykke.Service.Zcash.Api.Core.Domain.Events;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;
using MoreLinq;

namespace Lykke.Service.Zcash.Api.AzureRepositories
{
    public class PendingEventRepository : IPendingEventRepository
    {
        private INoSQLTableStorage<PendingEvent> _tableStorage;
        private static string GetPartitionKey(EventType eventType) => Enum.GetName(typeof(EventType), eventType);
        private static string GetRowKey(Guid operationId) => operationId.ToString();

        public PendingEventRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _tableStorage = AzureTableStorage<PendingEvent>.Create(connectionStringManager, "ZcashPendingEvents", log);
        }

        public async Task<IEnumerable<IPendingEvent>> Get(EventType eventType, int? limit = 99999)
        {
            return await _tableStorage.GetTopRecordsAsync(GetPartitionKey(eventType), limit.Value);
        }

        public async Task Delete(EventType eventType, IEnumerable<Guid> operationIds)
        {
            foreach (var batch in operationIds.Batch(100))
            {
                var batchOperation = new TableBatchOperation();
                var now = DateTime.Now.ToUniversalTime();

                foreach (var id in batch)
                {
                    batchOperation.Merge(new PendingEvent(GetPartitionKey(eventType), GetRowKey(id))
                    {
                        DeletedUtc = now
                    });
                }
                    
                await _tableStorage.DoBatchAsync(batchOperation);
            }
        }

        public async Task Create(EventType eventType, Guid id,
            string fromAddress,
            string assetId,
            string amount,
            string toAddress,
            string transactionHash)
        {
            await _tableStorage.InsertOrReplaceAsync(new PendingEvent(GetPartitionKey(eventType), GetRowKey(id))
            {
                FromAddress = fromAddress,
                AssetId = assetId,
                Amount = amount,
                ToAddress = toAddress,
                TransactionHash = transactionHash,
                CreatedUtc = DateTime.Now.ToUniversalTime()
            });
        }
    }
}
