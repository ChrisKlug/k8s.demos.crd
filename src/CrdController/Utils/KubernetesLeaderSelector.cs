using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CrdController.Utils
{
    public class KubernetesLeaderSelector : ILeaderSelector
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<KubernetesLeaderSelector> _logger;
        private readonly string _endpoint;

        public KubernetesLeaderSelector(IHttpClientFactory httpClientFactory, IHostEnvironment hostEnvironment, ILogger<KubernetesLeaderSelector> logger) 
            : this(httpClientFactory, hostEnvironment, logger, "http://localhost:4040")
        {
        }
        public KubernetesLeaderSelector(IHttpClientFactory httpClientFactory, IHostEnvironment hostEnvironment, ILogger<KubernetesLeaderSelector> logger, string endpoint)
        {
            _httpClientFactory = httpClientFactory;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
            _endpoint = endpoint;
        }

        public async Task<bool> IsLeader()
        {
            _logger.LogInformation("Getting leader information from " + _endpoint);
            var client = _httpClientFactory.CreateClient();
            string response;
            try 
            { 
                response = await client.GetStringAsync(_endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not reach leader selection endpoint");
                return false;
            }
            _logger.LogInformation("Got leader information " + response);

            return response.Contains(_hostEnvironment.EnvironmentName);
        }
    }
}
