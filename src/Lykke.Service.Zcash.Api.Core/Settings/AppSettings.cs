using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Zcash.Api.Core.Settings.SlackNotifications;

namespace Lykke.Service.Zcash.Api.Core.Settings
{
    public class AppSettings
    {
        public ZcashApiSettings ZcashApi { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
