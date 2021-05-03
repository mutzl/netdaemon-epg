using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public class SampleDataProviderService : IDataProviderService
    {
        public string ProviderName => "Sample";

        private readonly AppLogger<SampleDataProviderService> logger;

        private readonly HttpClient httpClient;


        public SampleDataProviderService(AppLogger<SampleDataProviderService> logger)
        {
            this.logger = logger;

            // most likely you will get your data via parsing http responses
            httpClient = new HttpClient { BaseAddress = new Uri("http://sample.url") };
        }

        public async Task<IEnumerable<Show>> LoadShowsAsync(string station)
        {
            var shows = new List<Show>();

            // most TV guides have "one day" defined from 6am to 6am.
            // so you might need to use yesterdays date, if it is already past midnight
            var today = (DateTime.Now.Hour >= 6) ? DateTime.Today : DateTime.Today.AddDays(-1);

            // it could be a strategy, to load program for today and tomorrow.
            shows.AddRange(await LoadShowsForADayAsync(station, today));
            shows.AddRange(await LoadShowsForADayAsync(station, today.AddDays(1)));

            // if the shows don't have already an end-time defined,
            // you could use a helper to set it based on the begin-time of the following show.
            SetEndTime(shows);

            return shows;
        }


        private async Task<IEnumerable<Show>> LoadShowsForADayAsync(string station, DateTime date)
        {
            var shows = new List<Show>();

            // insert here your http-request and some Regex magic...
            await Task.Delay(100);

            shows.Add(new Show("Morning Show", date.AddHours(6), station) { Category = "Talkshow", Id = "a" });
            shows.Add(new Show("High Noon", date.AddHours(12), station) { Category = "News" });
            shows.Add(new Show("Afternoon Talk", date.AddHours(13), station) { Category = "Talkshow", Id = "b" });
            shows.Add(new Show("Evening News", date.AddHours(19), station) { Category = "News" });
            shows.Add(new Show("Blockbuster", date.AddHours(20), station) { Category = "Movie", Id = "c" });
            shows.Add(new Show("Holy Moly", date.AddHours(23), station) { Category = "Movie", Id = "d" });
            shows.Add(new Show("Night News", date.AddHours(25), station) { Category = "News" });
            shows.Add(new Show("Bedtime Stories", date.AddHours(25.5), station) { Category = "Comedy", Id = "e" });

            logger.Log($"Loaded {station} TV programm for {date:d}");
            
            return shows;
        }

        private void SetEndTime(IEnumerable<Show> shows)
        {
            Show? lastShow = null;
            foreach (var show in shows)
            {
                if (lastShow != null) lastShow.End = show.Start;
                lastShow = show;
            }
        }

        public async Task<string> GetDescriptionAsMarkdown(Show show)
        {
            // load a detailed description of the show (mostlikely using the ID property).
            // some advanced Regex skills and you get your own Text using MarkDown.

            await Task.Delay(100);

            return $"{show.Start:HH:mm} - {show.Station}\r\n### {show.Title}\r\nLorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum.";

        }

    }
}
