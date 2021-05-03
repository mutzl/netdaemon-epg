using NetDaemon.Common.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public class StationGuide
    {
        private static int instanceCounter = 0;
        private IEnumerable<Show> guide = Enumerable.Empty<Show>();

        private readonly NetDaemonRxApp app;
        public string Station { get; }
        public IDataProviderService EpgService { get; }
        private readonly int refreshrate;
        private readonly IEnumerable<string> guideRefreshTimes;
        private readonly int instanceNumber;
        private Show? lastShow = null;
        private readonly Regex timeRegex = new Regex(@"^([0-1]?[0-9]|2[0-3]):([0-5][0-9])(?::([0-5][0-9]))?$");  // 09:30 / 9:30 / 09:30:00 / 9:30:00

        public StationGuide(NetDaemonRxApp app, string station, IDataProviderService epgService, int refreshrate, IEnumerable<string> guideRefreshTimes)
        {
            this.app = app;
            this.Station = station;
            this.EpgService = epgService;
            this.refreshrate = refreshrate;
            this.guideRefreshTimes = guideRefreshTimes;
            this.instanceNumber = instanceCounter++;
        }

        private string? FormatTime(string time)
        {
            var match = timeRegex.Match(time);
            if (!match.Success)
            {
                app.LogError($"'{time}' is not a valid Time.");
                return null;
            }

            if (time[1] == ':') time = "0" + time; // adding leading zero 

            if (time.Length == 5)
            {
                var seconds = instanceNumber * 2 % 60;
                time += $":{seconds:00}";  // adding seconds
            }
            return time;
        }

        public async Task InitializeAsync()
        {
            await RefreshGuideAsync();

            foreach (var refreshTime in guideRefreshTimes)
            {
                var time = FormatTime(refreshTime);
                if (time == null) continue;
                app.RunDaily(time, () => RefreshGuideAsync().Await(null, null));
            }

            app.RunEvery(TimeSpan.FromSeconds(refreshrate), () => GetCurrentShowAndSetSensorAsync().Await(null, null));
        }

        public async Task RefreshGuideAsync()
        {
            try
            {
                guide = await EpgService.LoadShowsAsync(Station);
                lastShow = null;
                ClearSensor(GetSensorName(Station));
                await GetCurrentShowAndSetSensorAsync();
            }
            catch (Exception ex)
            {
                app.LogError(ex, $@"Error when trying to refresh ""{Station}"" [EPG: {EpgService.ProviderName}]:\r\n{ex.Message}.");
            }
        }

        public Show? GetCurrentShow(DateTime now) => guide.Where(s => s.Start <= now).OrderByDescending(s => s.Start).FirstOrDefault();

        public Show? GetUpcomingShow(DateTime now) => guide.Where(s => s.Start > now).OrderBy(s => s.Start).FirstOrDefault();

        public async Task<string> GetDescription(Show show) => await EpgService.GetDescriptionAsMarkdown(show);

        private async Task GetCurrentShowAndSetSensorAsync()
        {
            var now = DateTime.Now;
            var currentShow = GetCurrentShow(now);
            var upcomingShow = GetUpcomingShow(now);

            var sensorName = GetSensorName(Station);

            // Clear Sensor, if no current show found
            if (currentShow == null)
            {
                app.Log($@"Cannot find current TV show for station ""{Station}"" [EPG: {EpgService.ProviderName}]");
                ClearSensor(sensorName);
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
                    Description = "loading...",
                }, true);

                app.Log($@"{EpgService.ProviderName} / {Station}: TV Show ""{state?.State}"" started at {currentShow.Start.ToShortTimeString()}");

                try
                {
                    if (state?.Attribute?["BeginTime"] != null)
                    {
                        lastShow = currentShow;  // setting state and attribute went ok
                        app.LogDebug($@"{EpgService.ProviderName} / {Station}: TV Show ""{state?.State}"" setting properties was successful");
                    }
                    else
                    {
                        lastShow = null; // make sure, sensor will be set at next run again.
                        app.LogDebug($@"{EpgService.ProviderName} / {Station}: TV Show ""{state?.State}"" need to set properties again.");
                    }
                }
                catch
                {
                    lastShow = null;  // make sure, sensor will be set at next run again.
                    app.LogDebug($@"{EpgService.ProviderName} / {Station}: TV Show ""{state?.State}"" need to set properties again.");
                }

                var description = await GetDescription(currentShow);
                if (state?.Attribute != null)
                {
                    state.Attribute["Description"] = description;
                    app.SetState(sensorName, state.State, state.Attribute);
                    app.Log($@"{EpgService.ProviderName} / {Station}: description for ""{state?.State}"" loaded");
                }
            }

            app.LogDebug($@"{EpgService.ProviderName} / {Station}: ""{currentShow.Title}"" running since {currentShow?.Start.ToShortTimeString()} {currentShow?.Title ?? "-"} ({currentShow?.DurationInPercent:P1})");
        }

        private string GetSensorName(string station) => $"sensor.epg_{EpgService.ProviderName.ToSimple()}_{station.ToSimple()}";

        private void ClearSensor(string sensorName)
        {
            app.SetState(sensorName, "", new { });
        }
    }
}
