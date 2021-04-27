using NetDaemon.Common;
using NetDaemon.Common.Reactive;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public class Epg : NetDaemonRxApp
    {
        private readonly int defaultRefreshrate = 30;
        private List<SenderGuide> senderGuides;

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

            senderGuides = new List<SenderGuide>();

            foreach (var senderName in Sender)
            {
                var sender = SenderItem.GetByName(senderName);
                if (sender == null)
                {
                    Log($"Station {senderName} not found in List");
                    continue;
                };

                var senderGuide = new SenderGuide(this, sender, hoerzuEpgService, RefreshrateInSeconds ?? defaultRefreshrate);
                senderGuides.Add(senderGuide);
                await senderGuide.InitializeAsync();
            }

            Log(nameof(Epg) + " initialized");
        }

        [HomeAssistantServiceCall]
        public async Task RefreshEpgData(dynamic data)
        {
            foreach (var senderGuide in senderGuides)
            {
                await senderGuide.RefreshGuideAsync();
            }

            Log(nameof(Epg) + " data refreshed");
        }
    }
}
