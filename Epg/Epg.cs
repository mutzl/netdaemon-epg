using NetDaemon.Common;
using NetDaemon.Common.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public class Epg : NetDaemonRxApp
    {
        private readonly int defaultRefreshrate = 30;
        private List<StationGuide> stationGuides = new List<StationGuide>();

        // Properties from yaml
        public int? RefreshrateInSeconds { get; set; }
        public IEnumerable<DataProvider>? DataProviders { get; set; }

        public override async Task InitializeAsync()
        {

            if (DataProviders == null)
            {
                LogError("No EPG data providers configured.");
                return;
            }
            
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            foreach (var dataProvider in DataProviders)
            {
                if (dataProvider.Fullname == null || dataProvider.Stations == null || !dataProvider.Stations.Any())
                {
                    LogError($"EPG data provider must contain a fullname.");
                    continue;
                }

                if (dataProvider.Stations == null || !dataProvider.Stations.Any())
                {
                    LogError($"EPG data provider {dataProvider.Fullname} doesn't contain any stations.");
                    continue;
                }

                var epgService = assembly?.CreateInstance(dataProvider.Fullname, 
                    true, System.Reflection.BindingFlags.Default, binder: null, args: new object[] { this }, null, null) as IDataProviderService;

                if (epgService == null)
                {
                    LogError($@"Could not create instance of EPG data provider ""{dataProvider.Fullname}"".");
                    continue;
                }

                foreach (var station in dataProvider.Stations)
                {
                    var senderGuide = new StationGuide(this, station, epgService, RefreshrateInSeconds ?? defaultRefreshrate);
                    stationGuides.Add(senderGuide);
                    await senderGuide.InitializeAsync();

                    Log($"{dataProvider.Fullname} - {station} initialized.");
                }
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
            foreach (var senderGuide in stationGuides)
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
            var now = DateTime.Now;

            var entityId = (string)data.entity_id;
            var regex = new Regex("sensor.epg_(.*?)_(.*)");

            var match = regex.Match(entityId);

            var dataProvider = match.Groups[1].Value;
            var station = match.Groups[2].Value;

            if (dataProvider.IsNullOrEmpty() || station.IsNullOrEmpty())
            {
                LogError($"Cannot find data provider or station for {entityId}");
                return;
            }

            var guide = stationGuides.SingleOrDefault(sg => sg.Station.ToSimple().Equals(station, StringComparison.InvariantCultureIgnoreCase) 
                                                         && sg.EpgService.ProviderName.ToSimple().Equals(dataProvider));
            if (guide == null)
            {
                LogError($"Could not find station guide for {station} with {dataProvider} data provider.");
                return;
            }

            var currentShow = guide.GetCurrentShow(now);
            if (currentShow == null || currentShow.Id == null)
            {
                LogError($"Could not find current tv show for {station} with {dataProvider} data provider.");
                return;
            }

            var description = await guide.GetDescription(currentShow);
            
            var state = SetState("sensor.epg_desc", currentShow.Title ?? station, new { description = description }, true);
        }

        
    }
}
