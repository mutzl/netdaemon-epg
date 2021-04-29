using NetDaemon.Common.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public class StationGuide
    {
        private IEnumerable<Show> guide = Enumerable.Empty<Show>();

        private readonly NetDaemonRxApp app;
        public string Station { get; }
        public IDataProviderService EpgService { get; }
        private readonly int refreshrate;

        private Show? lastShow = null;

        public StationGuide(NetDaemonRxApp app, string station, IDataProviderService epgService, int refreshrate)
        {
            this.app = app;
            this.Station = station;
            this.EpgService = epgService;
            this.refreshrate = refreshrate;
        }

        public async Task InitializeAsync()
        {
            await RefreshGuideAsync();
            GetCurrentShowAndSetSensor();

            app.RunDaily("06:30:00", () => RefreshGuideAsync().Await(null, null));
            app.RunEvery(TimeSpan.FromSeconds(refreshrate), GetCurrentShowAndSetSensor);
        }

        public async Task RefreshGuideAsync()
        {
            try
            {
                guide = await EpgService.LoadShowsAsync(Station);
            }
            catch (Exception ex)
            {
                app.LogError(ex, ex.Message);
            }
        }

        public Show? GetCurrentShow(DateTime now) => guide.Where(s => s.Start <= now).OrderByDescending(s => s.Start).FirstOrDefault();

        public Show? GetUpcomingShow(DateTime now) => guide.Where(s => s.Start > now).OrderBy(s => s.Start).FirstOrDefault();

        public async Task<string> GetDescription(Show show) => await EpgService.GetDescriptionAsMarkdown(show);

        private void GetCurrentShowAndSetSensor()
        {
            var now = DateTime.Now;
            var currentShow = GetCurrentShow(now);
            var upcomingShow = GetUpcomingShow(now);

            var sensorName = GetSensorName(Station);

            // Clear Sensor, if no current show found
            if (currentShow == null)
            {
                app.Log($"Cannot find current TV show for station {Station}.");
                app.SetState(sensorName, "", new { });
                return;
            }

            if (lastShow == null || !lastShow.Title.Equals(currentShow.Title, StringComparison.Ordinal) || !lastShow.Start.Equals(currentShow.Start))
            {
                var state = app.SetState(sensorName, currentShow.Title ?? "", new
                {
                    Station = Station,
                    Title = currentShow.Title,
                    Episode = currentShow.Episode,
                    BeginTime = currentShow.Start.ToShortTimeString(),
                    Duration = currentShow.DurationInMinutes,
                    Genre = currentShow.Category,
                    Upcoming = upcomingShow?.Title ?? string.Empty,
                    DataProvider = EpgService.ProviderName,
                    //Id = currentShow.Id,
                }, true);

                app.Log($"new TV show {state?.EntityId} {state?.State} started at {state?.Attribute?["BeginTime"]}");

                lastShow = currentShow;
            }

            app.LogDebug($"{Station} [{EpgService.ProviderName}]: seit {currentShow?.Start.ToShortTimeString()} {currentShow?.Title ?? "-"} {currentShow?.DurationInPercent:P1}");
        }

        private string GetSensorName(string station) => $"sensor.epg_{EpgService.ProviderName.ToSimple()}_{station.ToSimple()}";
    }
}
