using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public interface IEpgService
    {
        Task<IEnumerable<Show>> LoadShowsAsync(SenderItem sender);
    }
}