using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public interface IDataProviderService
    {
        string ProviderName { get; }
        Task<IEnumerable<Show>> LoadShowsAsync(string station);
        Task<string> GetDescriptionAsMarkdown(Show show);
    }
}