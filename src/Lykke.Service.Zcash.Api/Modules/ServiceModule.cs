using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Service.BlockchainSignService.Client;
using Lykke.Service.Zcash.Api.AzureRepositories;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Zcash.Api.Services;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.Zcash.Api.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<ZcashApiSettings> _settings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public ServiceModule(IReloadingManager<ZcashApiSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            // TODO: Do not register entire settings in container, pass necessary settings to services which requires them
            // ex:
            //  builder.RegisterType<QuotesPublisher>()
            //      .As<IQuotesPublisher>()
            //      .WithParameter(TypedParameter.From(_settings.CurrentValue.QuotesPublication))

            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            builder.RegisterBlockchainSignServiceClient(_settings.CurrentValue.SignApiUrl, _log);

            builder.RegisterType<InsightClient>()
                .As<IInsightClient>();

            builder.RegisterType<PendingEventRepository>()
                .As<IPendingEventRepository>()
                .WithParameter(TypedParameter.From(_settings.Nested(s => s.Db.DataConnString)));

            builder.RegisterType<BlockchainService>()
                .As<IBlockchainService>();

            // TODO: Add your dependencies here

            builder.Populate(_services);
        }
    }
}
