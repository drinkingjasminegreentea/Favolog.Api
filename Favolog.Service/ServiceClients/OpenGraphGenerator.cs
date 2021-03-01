using Favolog.Service.Models;
using Favolog.Service.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Favolog.Service.ServiceClients
{
    public class OpenGraphGenerator: IOpenGraphGenerator
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _settings;

        public OpenGraphGenerator(HttpClient client, IOptions<AppSettings> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            _settings = options.Value;
            _httpClient = client;
        }

        public async Task<OpenGraphData> GetOpenGraph(string url)
        {
            var response = await _httpClient.GetAsync($"{_settings.OpenGraphGeneratorUrl}{url}");
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<OpenGraphData>(json);
        }
    }
}
