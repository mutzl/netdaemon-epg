using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public class OrfDataProviderService : IDataProviderService
    {
        private readonly string baseUri = "https://tv.orf.at/program/";

        private readonly HttpClient httpClient;
        private readonly AppLogger<OrfDataProviderService> logger;

        public string ProviderName => "ORF";

        public OrfDataProviderService(AppLogger<OrfDataProviderService> logger)
        {
            this.logger = logger;
            httpClient = new HttpClient { BaseAddress = new Uri(baseUri) };
        }

        public async Task<IEnumerable<Show>> LoadShowsAsync(string station)
        {
            var shows = new List<Show>();

            var today = (DateTime.Now.Hour >= 6) ? DateTime.Today : DateTime.Today.AddDays(-1);
            
            shows.AddRange(await LoadShowsForADayAsync(station, today));
            shows.AddRange(await LoadShowsForADayAsync(station, today.AddDays(1)));

            SetEndTime(shows);

            return shows;
        }
           
        private string? GetShortStationName(string station)
        {
            var shortStation = station
                .Replace(" ", "")
                .Replace("+", "plus")
                .ToLower();

            var validStations = new[] { "orf1", "orf2", "orf3", "orfsportplus" };

            if (!validStations.Contains(shortStation))
            {
                logger.LogError($"{station} is not a valid ORF station.");
                return null;
            }
            return shortStation;
        }

        private async Task<IEnumerable<Show>> LoadShowsForADayAsync(string station, DateTime date)
        {
            var shows = new List<Show>();

            var name = GetShortStationName(station);

            var url = $"{name}/{date:yyyyMMdd}";
            var culture = new CultureInfo("de-DE");

            try
            {
                var response = await httpClient.GetAsync(url);
                var html = await response.Content.ReadAsStringAsync();

                var pattern = @"<div class=""starttime"">\s*<h3 class="".*?"">(.*?)<\/h3>\s*(?:<p class=""genre"">(.*?)<\/p>)?\s*<\/div>\s*<div class=""broadcast"">.*?<div class=""teaser"">.*?<h2 class="".*?"">(.*?)<\/h2>\s*(?:<h3>(.*?)<\/h3>)?.*?(?:<p class=""detaillink""><a href=""(.*?)"">mehr...<\/a><\/p>)?\s*<\/div>\s*<div class=""info"">(.*?)<\/div>";

                var matches = Regex.Matches(html, pattern, RegexOptions.Singleline);

                var lastStart = date;

                foreach (Match match in matches)
                {
                    // Date + "20:15" => StartTime
                    var start = date.Add(TimeSpan.Parse(match.Groups[1].Value, culture));
                    // After midnight => next day
                    if (start < lastStart) start = start.AddDays(1);
                    lastStart = start;

                    var link = match.Groups[5].Value;

                    // https://tv.orf.at/program/orf3/20210428/985355401/
                    // https://tv.orf.at/program/orf3/20210428/985355401/story
                    var linkMatch = Regex.Match(link, $@"https:\/\/tv.orf.at\/program\/{name}\/(\d*\/\d*).*?");
                    var id = linkMatch.Groups[1].Value;

                    var show = new Show(match.Groups[3].Value, start, station)
                    {
                        Id = id,
                        Episode = match.Groups[4].Value,
                        Category = match.Groups[2].Value,
                    };

                    shows.Add(show);
                }


                logger.Log($"Loaded {station} TV programm for {date:d}");

                return shows;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error loading {station} TV program for {date:d} with ORF data provider.");
                return shows;
            }
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
            if (show == null || show.Station.IsNullOrEmpty() || show.Id.IsNullOrEmpty())
            {
                logger.Log("Not enough information to get description for show.");
                return "Nicht verfügbar.";
            }

            var response = await httpClient.GetAsync($"{show.Station.ToSimple()}/{show.Id}/");
            var html = await response.Content.ReadAsStringAsync();
            
            var pattern = @"<div class=""main clear"">\s*?<div class=""starttime"">\s*?<h3 class="".*?"">(.*?)<\/h3>\s*?<p class=""genre"">(.*?)<\/p>\s*?<div class=""status"">\s*?(.*?)<\/div>.*?<div class=""broadcasttitle"">.*?<h2 class="".*?"">(.*?)<\/h2>\s*?(?:<h3>(.*?)<\/h3>)?.*?<div class=""paragraph"">(.*?)<div class=""navigation""";
            var regex = new Regex(pattern, RegexOptions.Singleline);
            var match = regex.Match(html);

            var time = match.Groups[1].Value;
            var genre = match.Groups[2].Value;
            var details= match.Groups[3].Value;
            var title = match.Groups[4].Value;
            var episode = match.Groups[5].Value;
            var text = match.Groups[6].Value;

            match = Regex.Match(text, @"<p>(.*?)<\/p>(.*)", RegexOptions.Singleline);
            text = match.Groups[1].Value;
            var crew = Regex.Match(match.Groups[2].Value, @"<div class=""staff"">(.*?)<\/div>").Groups[1].Value;

            var sender = show.Station;

            html = $"{time} - {sender}\r\n### {title}\r\n#### {episode}\r\n{text}\r\n\r\n{crew}";

            html = html.Replace("<br>", "\r\n", StringComparison.OrdinalIgnoreCase)
                .Replace("<br/>", "\r\n", StringComparison.OrdinalIgnoreCase)
                .Replace("<br />", "\r\n", StringComparison.OrdinalIgnoreCase)
                .Replace("<p>", "\r\n", StringComparison.OrdinalIgnoreCase)
                .Replace("</p>", "\r\n", StringComparison.OrdinalIgnoreCase);

            var result = Regex.Replace(html, @"<[^>]*>", string.Empty);
            return result;
        }

    }
}
