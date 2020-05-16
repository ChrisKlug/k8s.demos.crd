using CrdController.Models;
using k8s;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CrdController
{
    public class CrdControllerService : IHostedService
    {
        private readonly IKubernetes _kubernetes;
        private readonly IFooList _fooList;
        private readonly ILogger<CrdControllerService> _logger;
        private Watcher<Foo> _watcher;

        public CrdControllerService(IKubernetes kubernetes, IFooList fooList, ILogger<CrdControllerService> logger)
        {
            _kubernetes = kubernetes;
            _fooList = fooList;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Foo monitoring...");

            _logger.LogInformation("Getting existing Foos...");
            var result = (JObject)(await _kubernetes.ListNamespacedCustomObjectAsync("demos.fearofoblivion.com", "v1", "default", "foos"));
            var foos = result.ToObject<Models.FooList>();
            foreach (var foo in foos.Items)
            {
                _logger.LogInformation("Found existing foo \"" + foo.Metadata.Name + "\"\r\n" + GetFooData(foo));
                _fooList.Add(foo);
            }
            _logger.LogInformation("Foo listing complete! Starting monitoring...");

            var fooListResponse = _kubernetes.ListNamespacedCustomObjectWithHttpMessagesAsync("demos.fearofoblivion.com", "v1", "default", "foos", watch: true);
            _watcher = fooListResponse.Watch<Foo, object>(OnFooChange);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Foo monitoring!");

            _watcher.Dispose();

            return Task.CompletedTask;
        }

        private void OnFooChange(WatchEventType type, Foo item)
        {
            if (type == WatchEventType.Added && _fooList.Foos.Any(x => x.Metadata.Name == item.Metadata.Name))
            {
                return;
            }


            if (type == WatchEventType.Added)
            {
                _logger.LogInformation($"Foo \"{item.Metadata.Name}\" {type.ToString().ToLower()}\r\n{GetFooData(item)}");
                _fooList.Add(item);
                return;
            }
            else if (type == WatchEventType.Deleted)
            {
                _fooList.Remove(item);
            }

            _logger.LogInformation($"Foo \"{item.Metadata.Name}\" {type.ToString().ToLower()}");
        }

        private static string GetFooData(Foo item)
        {
            return $"Name: \"{item.Metadata.Name}\"\r\nProperties:" +
                    $"\r\n\tValue1: {item.Spec.Value1}" +
                    $"\r\n\tValue2: {item.Spec.Value2}" +
                    $"\r\n\tValue3: {item.Spec.Value3}" +
                    $"\r\n\tValue4: {item.Spec.Value4}";
        }
    }
}
