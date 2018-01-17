using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.Zcash.Api.Core.Domain.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.Zcash.Api.AzureRepositories.Settings
{
    public class SettingsRepository : ISettingsRepository
    {
        private INoSQLTableStorage<SettingsEntity> _tableStorage;
        private static string GetPartitionKey() => "Settings";
        private static string GetRowKey() => "";

        public SettingsRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _tableStorage = AzureTableStorage<SettingsEntity>.Create(connectionStringManager, "ZcashSettings", log);
        }

        public async Task<ISettings> GetAsync()
        {
            return await _tableStorage.GetDataAsync(GetPartitionKey(), GetRowKey());
        }

        public async Task UpsertAsync(ISettings settings)
        {
            var entity = new SettingsEntity(GetPartitionKey(), GetRowKey())
            {
                ConfirmationLevel = settings.ConfirmationLevel,
                LastBlockHash = settings.LastBlockHash,
                FeePerKb = settings.FeePerKb,
                MaxFee = settings.MaxFee,
                MinFee = settings.MinFee,
                UseDefaultFee = settings.UseDefaultFee
            };

            await _tableStorage.InsertOrReplaceAsync(entity);
        }
    }
}
