namespace Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings
{
    public class ZcashApiSettings
    {
        public DbSettings Db { get; set; }
        public string SignApiUrl { get; set; }
        public string InsightUrl { get; set; }
        public string[] SourceWallets { get; set; }
        public decimal FeeRate { get; set; }
        public decimal MinFee { get; set; }
        public decimal MaxFee { get; set; }
        public bool UseDefaultFee { get; set; }
        public bool EnableRbf { get; set; }
    }
}
