using CrdController.Models;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CrdController.Services
{
    public class CrdControllerService : IHostedService
    {
        private readonly IKubernetes _kubernetes;
        private readonly IFooList _fooList;
        private readonly ILeaderSelector _leaderSelector;
        private readonly ILogger<CrdControllerService> _logger;
        private Watcher<Foo> _watcher;

        public CrdControllerService(IKubernetes kubernetes, IFooList fooList, ILeaderSelector leaderSelector, ILogger<CrdControllerService> logger)
        {
            _kubernetes = kubernetes;
            _fooList = fooList;
            _leaderSelector = leaderSelector;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Foo monitoring...");
            
            var fooListResponse = _kubernetes.ListNamespacedCustomObjectWithHttpMessagesAsync(Foo.Group, Foo.Version, "default", Foo.Plural, watch: true);

            _watcher = fooListResponse.Watch<Foo, object>((type, item) => OnFooChange(type, item));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Foo monitoring!");

            _watcher.Dispose();

            return Task.CompletedTask;
        }

        private async Task OnFooChange(WatchEventType type, Foo item)
        {
            var isLeader = await _leaderSelector.IsLeader();
            if (!isLeader)
            {
                _logger.LogInformation($"Foo \"{item.Metadata.Name}\" {type.ToString().ToLower()} - Not leader, ignoring change!");
            }

            switch (type)
            {
                case WatchEventType.Added:
                    await (isLeader ? OnFooAdded(item) : Task.CompletedTask);
                    _fooList.Add(item);
                    return;
                case WatchEventType.Modified:
                    await (isLeader ? OnFooUpdated(item) : Task.CompletedTask);
                    _fooList.Update(item);
                    return;
                case WatchEventType.Deleted:
                    await (isLeader ? OnFooDeleted(item) : Task.CompletedTask);
                    _fooList.Remove(item);
                    return;
            };
        }

        private async Task OnFooAdded(Foo foo)
        {
            if (foo.Status != null)
            {
                return;
            }

            _logger.LogInformation($"Foo \"{foo.Metadata.Name}\" added\r\n{GetFooData(foo)}");

            await UpdateStatus(foo, FooStatuses.Initializing);

            new Timer(x => {
                UpdateStatus(foo, FooStatuses.Complete);
            }, null, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));

        }
        private Task OnFooUpdated(Foo foo)
        {
            var preChangeFoo = _fooList.Find(foo.Metadata.Name);
            if (preChangeFoo.Status != foo.Status)
            {
                _logger.LogInformation($"Foo \"{foo.Metadata.Name}\" changed status to {foo.Status}");
            }
            else
            {
                _logger.LogInformation($"Foo \"{foo.Metadata.Name}\" updated\r\n{GetFooData(foo)}");
            }

            // Look for other changes

            return Task.CompletedTask;
        }
        private Task OnFooDeleted(Foo foo)
        {
            _logger.LogInformation($"Foo \"{foo.Metadata.Name}\" deleted");

            return Task.CompletedTask;
        }

        private async Task UpdateStatus(Foo foo, string status)
        {
            var patch = new JsonPatchDocument<Foo>();
            patch.Add(x => x.Status, status);
            var response = await _kubernetes.PatchNamespacedCustomObjectAsync(new V1Patch(patch), Foo.Group, Foo.Version, "default", Foo.Plural, foo.Metadata.Name);
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
