using System;

namespace Mutzl.Homeassistant
{
    public class Show
    {
        public Show(string title, DateTime start, string station)
        {
            Title = title;
            Start = start;
            Station = station;
        }

        public string? Id { get; set; }
        public string Title { get; set; }
        public string? Episode { get; set; }
        public DateTime Start { get; set; }
        public string Station { get; }
        public DateTime? End { get; set; }
        public string? Category { get; set; }


        public int? DurationInMinutes
        {
            get
            {
                if (End == null) return null;
                var duration = End - Start;
                return (int)duration.Value.TotalMinutes;
            }
        }

        public double? DurationInPercent
        {
            get
            {
                var totalDuration = DurationInMinutes;
                if (totalDuration == null) return null;

                var now = DateTime.Now;
                if (Start > now) return 0.0;

                var duration = (now - Start).TotalMinutes;
                return duration / totalDuration;
            }
        }

        public override string ToString()
        {
            return $"{Title} - {Start:HH:mm}";
        }
    }
}
