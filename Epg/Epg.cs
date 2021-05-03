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
        private readonly IEnumerable<string> defaultGuideRefreshTimes = new[] { "06:30" };

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

                var epgService = CreateDataProviderService(dataProvider.Fullname);

                if (epgService == null)
                {
                    LogError($@"Could not create instance of EPG data provider ""{dataProvider.Fullname}"".");
                    continue;
                }

                foreach (var station in dataProvider.Stations)
                {
                    var senderGuide = new StationGuide(this, station, epgService, RefreshrateInSeconds ?? defaultRefreshrate, dataProvider.RefreshTimes ?? defaultGuideRefreshTimes);
                    stationGuides.Add(senderGuide);
                    await senderGuide.InitializeAsync();

                    Log($"{dataProvider.Fullname} - {station} initialized.");
                }
            }

            Log("EPG App fully initialized");
        }

        private IDataProviderService? CreateDataProviderService(string fullname)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            Type loggerType = typeof(AppLogger<>);
            Type? dataProviderServiceType = assembly.GetType(fullname);
            if (dataProviderServiceType == null) return null;

            Type constructed = loggerType.MakeGenericType(dataProviderServiceType);
            var logger = Activator.CreateInstance(constructed, this);

            return Activator.CreateInstance(dataProviderServiceType, logger) as IDataProviderService;
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

            Log("EPG data refreshed.");
        }

    }
}
