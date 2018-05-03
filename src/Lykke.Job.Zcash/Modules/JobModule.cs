using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Job.Zcash.PeriodicalHandlers;
using Lykke.Service.Zcash.Api.AzureRepositories.Addresses;
using Lykke.Service.Zcash.Api.AzureRepositories.History;
using Lykke.Service.Zcash.Api.AzureRepositories.Operations;
using Lykke.Service.Zcash.Api.AzureRepositories.Settings;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.History;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;
using Lykke.Service.Zcash.Api.Core.Domain.Settings;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Zcash.Api.Services;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using NBitcoin.RPC;
using NBitcoin.Zcash;

namespace Lykke.Job.Zcash.Modules
{
    public class JobModule : Module
    {
        private readonly IReloadingManager<ZcashApiSettings> _settings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public JobModule(IReloadingManager<ZcashApiSettings> settings, ILog log)
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

            builder.RegisterType<AddressRepository>()
                .As<IAddressRepository>()
                .WithParameter(TypedParameter.From(_settings.Nested(s => s.Db.DataConnString)))
                .SingleInstance();

            builder.RegisterType<HistoryRepository>()
                .As<IHistoryRepository>()
                .WithParameter(TypedParameter.From(_settings.Nested(s => s.Db.DataConnString)))
                .SingleInstance();

            builder.RegisterType<OperationRepository>()
                .As<IOperationRepository>()
                .WithParameter(TypedParameter.From(_settings.Nested(s => s.Db.DataConnString)))
                .SingleInstance();

            builder.RegisterType<SettingsRepository>()
                .As<ISettingsRepository>()
                .WithParameter(TypedParameter.From(_settings.Nested(s => s.Db.DataConnString)))
                .SingleInstance();

            ZcashNetworks.Instance.EnsureRegistered();

            builder.RegisterInstance(Network.GetNetwork(_settings.CurrentValue.NetworkType))
                .As<Network>();

            builder.RegisterType<RPCClient>()
                .AsSelf()
                .WithParameter("authenticationString", _settings.CurrentValue.RpcAuthenticationString)
                .WithParameter("hostOrUri", _settings.CurrentValue.RpcUrl);

            builder.RegisterType<BlockchainReader>()
                .As<IBlockchainReader>();

            builder.RegisterType<BlockchainService>()
                .As<IBlockchainService>()
                .WithParameter(TypedParameter.From(_settings.CurrentValue));

            RegisterPeriodicalHandlers(builder);

            builder.Populate(_services);
        }

        private void RegisterPeriodicalHandlers(ContainerBuilder builder)
        {
            builder.RegisterType<HistoryHandler>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.IndexInterval))
                .SingleInstance();
        }
    }
}
