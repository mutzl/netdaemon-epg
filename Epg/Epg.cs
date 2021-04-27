using NetDaemon.Common.Reactive;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public class Epg : NetDaemonRxApp
    {
        private readonly int defaultRefreshrate = 30;

        // Properties from yaml
        public int? RefreshrateInSeconds { get; set; }
        public IEnumerable<string>? Sender { get; set; }

        public override async Task InitializeAsync()
        {
            if (Sender == null)
            {
                LogError("No station configured for EPG.");
                return;
            }

            var hoerzuEpgService = new HoerzuEpgService(this);

            foreach (var senderName in Sender)
            {
                var sender = SenderItem.GetByName(senderName);
                if (sender == null)
                {
                    Log($"Station {senderName} not found in List");
                    continue;
                };

                var senderGuide = new SenderGuide(this, sender, hoerzuEpgService, RefreshrateInSeconds ?? defaultRefreshrate);
                await senderGuide.InitializeAsync();
            }

            Log(nameof(Epg) + " initialized");
        }
    }
}
