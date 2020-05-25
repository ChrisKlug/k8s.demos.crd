using CrdController.Models;
using CrdController.Services;
using CrdController.Utils;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CrdController.HostedServices
{
    public class CrdControllerService : IHostedService
    {
        private readonly IKubernetes _kubernetes;
        private readonly ResourceSet<Foo> _foos;
        private readonly ILeaderSelector _leaderSelector;
        private readonly ILogger<CrdControllerService> _logger;
        private Timer _leaderCheckTimer;
        private Watcher<Foo> _watcher;

        public CrdControllerService(IKubernetes kubernetes, ResourceSet<Foo> fooList, ILeaderSelector leaderSelector, ILogger<CrdControllerService> logger)
        {
            _kubernetes = kubernetes;
            _foos = fooList;
            _leaderSelector = leaderSelector;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting FooController...");

            _leaderCheckTimer = new Timer(async x => {

                var isLeader = await _leaderSelector.IsLeader();
                if (isLeader)
                {
                    _leaderCheckTimer.Dispose();
                    _leaderCheckTimer = null;
                    OnPromotedToLeader();
                }

            }, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(10));
            

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Foo monitoring!");

            _watcher.Dispose();

            return Task.CompletedTask;
        }

        private void OnPromotedToLeader()
        {
            _logger.LogInformation("Instance promoted to leader! Starting Foo monitoring...");

            var fooListResponse = _kubernetes.ListNamespacedCustomObjectWithHttpMessagesAsync(Foo.Group, Foo.Version, "default", Foo.Plural, watch: true);
            _watcher = fooListResponse.Watch<Foo, object>(async (type, item) => await OnFooChange(type, item));
        }

        private async Task OnFooChange(WatchEventType type, Foo item)
        {
            switch (type)
            {
                case WatchEventType.Added:
                    await OnFooAdded(item);
                    _foos.Add(item);
                    return;
                case WatchEventType.Modified:
                    await OnFooUpdated(item);
                    _foos.Update(item);
                    return;
                case WatchEventType.Deleted:
                    await OnFooDeleted(item);
                    _foos.Remove(item);
                    return;
            };
        }

        private async Task OnFooAdded(Foo foo)
        {
            if (foo.Status == "COMPLETE")
            {
                _logger.LogInformation($"Foo \"{foo.Metadata.Name}\" already completed. Ignoring...");
                return;
            }

            _logger.LogInformation($"Foo \"{foo.Metadata.Name}\" added\r\n{GetFooData(foo)}");

            await UpdateStatus(foo, "INITIALIZING");

            // TODO: Do whatever is needed when a Foo is added

            // Simulating a delay do to work being done!
            new Timer(async x => {
                await UpdateStatus(foo, "COMPLETE");
            }, null, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));

        }
        private Task OnFooUpdated(Foo foo)
        {
            var preChangeFoo = _foos.Find(foo.Metadata.Name);
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
            if (!foo.Metadata.Annotations.ContainsKey(Foo.StatusAnnotationName))
            {
                foo.Metadata.Annotations.Add(Foo.StatusAnnotationName, status);
            }
            else
            {
                foo.Metadata.Annotations[Foo.StatusAnnotationName] = status;
            }
            var patch = new JsonPatchDocument<Foo>();
            patch.Replace(x => x.Metadata.Annotations, foo.Metadata.Annotations);
            patch.Operations.ForEach(x => x.path = x.path.ToLower());
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
