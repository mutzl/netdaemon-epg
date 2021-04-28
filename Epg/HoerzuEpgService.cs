﻿using NetDaemon.Common.Reactive;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public class HoerzuEpgService : IEpgService
    {
        private readonly string baseUri = "https://www.hoerzu.de/text/tv-programm/";

        private readonly HttpClient httpClient;
        private readonly NetDaemonRxApp app;

        private const int TODAY = 0;
        private const int TOMORROW = 1;

        public HoerzuEpgService(NetDaemonRxApp app)
        {
            this.app = app;

            httpClient = new HttpClient { BaseAddress = new Uri(baseUri) };
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (compatible)");
        }

        public async Task<IEnumerable<Show>> LoadShowsAsync(SenderItem sender)
        {
            var shows = new List<Show>();
            
            shows.AddRange(await LoadShowsForADayAsync(sender, TODAY));
            shows.AddRange(await LoadShowsForADayAsync(sender, TOMORROW));

            SetEndTime(shows);

            return shows;
        }

        private async Task<IEnumerable<Show>> LoadShowsForADayAsync(SenderItem sender, int day)
        {
            var shows = new List<Show>();

            // find date on page
            // <h3>Sender ORF 1 für Mittwoch, den 28.04.2021 .</h3>
            var datePattern = @"<h3>.*?(\d{1,2}\.\d{1,2}\.\d{4}) \.<\/h3>";
            var dateRegex = new Regex(datePattern);
            var culture = new CultureInfo("de-DE");

            var response = await httpClient.PostAsync($"sender.php?newday={day}&tvchannelid={sender.Id}&timeday=ganztags", null);
            var html = await response.Content.ReadAsStringAsync();

            var dateMatch = dateRegex.Match(html);
            var date = dateMatch.Groups[1].Value;
            if (date == "") return shows;

            // find each entry
            // <a href="detail.php?broadcast_id=162482886&seite=s&timeday=ganztags&newday=1&tvchannelid=54">00:35 Uhr , House of Cards , Serie</a>
            var pattern = $@"<a href=""detail\.php\?broadcast_id=(.*?)&seite=s&timeday=ganztags&newday=\d&tvchannelid={sender.Id}"">(.*?) Uhr , (.*?) , (.*?)<\/a>";
            var regex = new Regex(pattern);
            var matches = regex.Matches(html);

            var lastStart = DateTime.Parse(date, culture);

            foreach (Match match in matches)
            {
                // Date + "20:15" => StartTime
                var start = DateTime.Parse($"{date} {match.Groups[2].Value}", culture);
                // After midnight => next day
                if (start < lastStart) start = start.AddDays(1);
                lastStart = start;

                var show = new Show
                {
                    Id = int.Parse(match.Groups[1].Value),
                    Title = match.Groups[3].Value,
                    Start = start,
                    Category = match.Groups[4].Value,
                };

                shows.Add(show);
            }

            app.Log($"Loaded {sender.Name} programm for {date}");

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

        public async Task<string> GetDescriptionAsMarkdown(int id)
        {
            var response = await httpClient.GetAsync($"detail.php?broadcast_id={id}");
            var html = await response.Content.ReadAsStringAsync();
            
            var match = Regex.Match(html, @"Zur&uuml;ck<\/a><BR\/><BR\/>(.*)<a href=""javascript:history\.back");
            html = match.Groups[1].Value;
            
            match = Regex.Match(html, @"<span class=""tabtextbold"">(.*?) Uhr , (.*?) , (.*?) . <\/span><br\/>(.*?)., (.*?)<br\/><br\/>(.*?)<p>(.*?)<\/p>(.*?)<BR\/>");
            var time = match.Groups[1].Value;
            var sender = match.Groups[2].Value;
            var title = match.Groups[3].Value;
            var sub = match.Groups[4].Value;
            var duration = match.Groups[5].Value;
            var details= match.Groups[6].Value.Replace(" , ", "");
            var text = match.Groups[7].Value;
            var crew = match.Groups[8].Value.Replace(" , ", "");

            html = $"{time} - {sender}\r\n### {title}\r\n{sub}, {duration}\r\n\r\n{text}\r\n\r\n{crew}\r\n\r\n{details}";

            html = html.Replace("<br>", "\r\n", StringComparison.OrdinalIgnoreCase)
                .Replace("<br/>", "\r\n", StringComparison.OrdinalIgnoreCase)
                .Replace("<p>", "\r\n", StringComparison.OrdinalIgnoreCase)
                .Replace("</p>", "\r\n", StringComparison.OrdinalIgnoreCase);

            var result = Regex.Replace(html, @"<[^>]*>", string.Empty);
            return result;
        }
    }
}
