using Lykke.Service.Zcash.Api.Core.Domain.Settings;

namespace Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings
{
    public class ZcashApiSettings : ISettings
    {
        public DbSettings Db { get; set; }
        public string RpcAuthenticationString { get; set; }
        public string RpcUrl { get; set; }
        public decimal FeePerKb { get; set; }
        public decimal MinFee { get; set; }
        public decimal MaxFee { get; set; }
        public bool UseDefaultFee { get; set; }
        public int ConfirmationLevel { get; set; }
        public int IndexInterval { get; set; }
        public string LastBlockHash { get; set; }
    }
}
