using System;

namespace Mutzl.Homeassistant
{
    public class Show
    {
        public int? Id { get; set; }
        public string? Title { get; set; }
        public DateTime Start { get; set; }
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
                var duration = (now - Start).TotalMinutes;
                return duration / totalDuration;
            }
        }
    }
}
