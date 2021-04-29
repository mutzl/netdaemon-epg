using System.Collections.Generic;

namespace Mutzl.Homeassistant
{
    public class DataProvider
    {
        public string? Fullname { get; set; }
        public IEnumerable<string>? Stations { get; set; }
    }
}
