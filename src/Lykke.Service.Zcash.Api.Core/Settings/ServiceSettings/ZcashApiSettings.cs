namespace Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings
{
    public class ZcashApiSettings
    {
        public DbSettings Db { get; set; }
        public string SignApiUrl { get; set; }
        public string[] SourceWallets { get; set; }
        public ulong FeePerByte { get; set; }
        public ulong MinFee { get; set; }
        public ulong MaxFee { get; set; }
        public bool UseDefaultFee { get; set; }
    }
}
