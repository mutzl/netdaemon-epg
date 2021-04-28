using NetDaemon.Common;
using NetDaemon.Common.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public class Epg : NetDaemonRxApp
    {
        private readonly int defaultRefreshrate = 30;
        private List<SenderGuide> senderGuides = new List<SenderGuide>();
        private IEpgService epgService;


        // Properties from yaml
        public int? RefreshrateInSeconds { get; set; }
        public IEnumerable<string>? Sender { get; set; }

        public Epg()
        {
            // if you want to use another data source, just implement your own IEpgService!
            // just need to provide 2 public methods LoadShowsAsync and GetDescriptionAsMarkdown
            epgService = new HoerzuEpgService(this);
        }

        public override async Task InitializeAsync()
        {
            if (Sender == null)
            {
                LogError("No station configured for EPG.");
                return;
            }

            foreach (var senderName in Sender)
            {
                var sender = SenderItem.GetByName(senderName);
                if (sender == null)
                {
                    Log($"Station {senderName} not found in List");
                    continue;
                };

                var senderGuide = new SenderGuide(this, sender, epgService, RefreshrateInSeconds ?? defaultRefreshrate);
                senderGuides.Add(senderGuide);
                await senderGuide.InitializeAsync();
            }

            Log(nameof(Epg) + " initialized");
        }

        /// <summary>
        /// Callback from homeassistant to refresh the EPG data.
        /// </summary>
        /// <example>
        ///   tap_action:
        ///     action: call-service
        ///     service: netdaemon.epg_refreshepgdata
        /// </example>
        [HomeAssistantServiceCall]
        public async Task RefreshEpgData(dynamic data)
        {
            foreach (var senderGuide in senderGuides)
            {
                await senderGuide.RefreshGuideAsync();
            }

            Log(nameof(Epg) + " data refreshed");
        }

        /// <summary>
        /// Callback from homeassistant to get the description of the current tv show for a given station in markdown format.
        /// Descrition is set to sensor.epg_desc as description attribute (due to 255 char size limit of state).
        /// </summary>
        /// <example>
        ///   hold_action:
        ///     action: call-service
        ///     service: netdaemon.epg_getdescription
        ///     service_data:
        ///       sender: ORF 1
        /// </example>
        [HomeAssistantServiceCall]
        public async Task GetDescription(dynamic data)
        {
            var senderName = (string)data.sender;

            var guide = senderGuides.SingleOrDefault(sg => sg.Sender.Name == senderName);
            if (guide == null) return;

            var currentShow = guide.GetCurrentShow(DateTime.Now);
            if (currentShow == null || !currentShow.Id.HasValue) return; 
            var description = await guide.GetDescription(currentShow.Id.Value);

            var state = SetState("sensor.epg_desc", currentShow.Title ?? senderName, new { description = description }, true);

            Log(nameof(Epg) + " description set");
        }
    }
}
