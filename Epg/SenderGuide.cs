using NetDaemon.Common.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public class SenderGuide
    {
        private IEnumerable<Show> guide = Enumerable.Empty<Show>();

        private readonly NetDaemonRxApp app;
        public SenderItem Sender { get;  }
        private readonly IEpgService epgService;
        private readonly int refreshrate;

        private Show? lastShow = null;

        public SenderGuide(NetDaemonRxApp app, SenderItem sender, IEpgService epgService, int refreshrate)
        {
            this.app = app;
            this.Sender = sender;
            this.epgService = epgService;
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
                guide = await epgService.LoadShowsAsync(Sender);
            }
            catch (Exception ex)
            {
                app.LogError(ex, ex.Message);
            }        
        }

        public Show? GetCurrentShow(DateTime now) => guide.Where(s => s.Start <= now).OrderByDescending(s => s.Start).FirstOrDefault();
        public Show? GetUpcomingShow(DateTime now) => guide.Where(s => s.Start > now).OrderBy(s => s.Start).FirstOrDefault();

        public async Task<string> GetDescription(int id) => await epgService.GetDescriptionAsMarkdown(id);

        private void GetCurrentShowAndSetSensor()
        {
            var now = DateTime.Now;
            var currentShow = GetCurrentShow(now);
            var upcomingShow = GetUpcomingShow(now);

            var sensorName = GetSensorName(Sender);

            // Clear Sensor, if no current show found
            if (currentShow == null)
            {
                app.Log($"Cannot find current TV show for station {Sender.Name}.");
                app.SetState(sensorName, "");
                return;
            }

            if (lastShow == null || !lastShow.Title.Equals(currentShow.Title, StringComparison.Ordinal) || !lastShow.Start.Equals(currentShow.Start))
            {
                var state = app.SetState(sensorName, currentShow.Title ?? "", new
                {
                    Sender = Sender.Name,
                    BeginTime = currentShow.Start.ToShortTimeString(),
                    Duration = currentShow.DurationInMinutes,
                    Genre = currentShow.Category,
                    Upcoming = upcomingShow?.Title ?? string.Empty,
                }, true);

                app.Log($"new TV show {state?.EntityId} {state?.State} started at {state?.Attribute?["BeginTime"]}");

                lastShow = currentShow;
            }

            app.LogDebug($"{Sender.Name}: seit {currentShow?.Start.ToShortTimeString()} {currentShow?.Title ?? "-"} {currentShow?.DurationInPercent:P1}");
            app.LogDebug($"{Sender.Name}: coming next: {upcomingShow?.Start.ToShortTimeString()} {upcomingShow?.Title ?? "-"} {upcomingShow?.DurationInMinutes} minutes");
        }

        private string GetSensorName(SenderItem sender)
        {
            var sensorName = sender.Name
                .ToLower()
                .Replace(" ", "")
                .Replace(".", "")
                .Replace("-", "")
                .Replace("+", "plus")
                ;

            return $"sensor.epg_{sensorName}";
        }
    }
}
