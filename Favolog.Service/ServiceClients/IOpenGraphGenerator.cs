using Favolog.Service.Models;
using System.Threading.Tasks;

namespace Favolog.Service.ServiceClients
{
    public interface IOpenGraphGenerator
    {
        Task<OpenGraphData> GetOpenGraph(string url);
    }
}
