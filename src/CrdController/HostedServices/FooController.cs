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
                    _foos.Add(item);
                    return;
                case WatchEventType.Modified:
                    await (isLeader ? OnFooUpdated(item) : Task.CompletedTask);
                    _foos.Update(item);
                    return;
                case WatchEventType.Deleted:
                    await (isLeader ? OnFooDeleted(item) : Task.CompletedTask);
                    _foos.Remove(item);
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
            if (!foo.Metadata.Annotations.ContainsKey("status"))
            {
                foo.Metadata.Annotations.Add("status", status);
            }
            else
            {
                foo.Metadata.Annotations["status"] = status;
            }
            var patch = new JsonPatchDocument<Foo>();
            patch.Replace(x => x.Metadata.Annotations, foo.Metadata.Annotations);
            patch.Add(x => x.Status, status);
            var response = await _kubernetes.PatchNamespacedCustomObjectAsync(new V1Patch(patch), Foo.Group, Foo.Version, "default", Foo.Plural, foo.Metadata.Name);

            //var fooRef = new V1ObjectReference(
            //        apiVersion: "apiextensions.k8s.io/v1beta1",
            //        kind: "foo",
            //        name: foo.Metadata.Name,
            //        namespaceProperty: "default",
            //        uid: foo.Metadata.Uid
            //    );
            //var fooOwnerRef = new V1OwnerReference(
            //        apiVersion: "apiextensions.k8s.io/v1beta1",
            //        kind: "foo",
            //        name: foo.Metadata.Name,
            //        uid: foo.Metadata.Uid
            //    );
            //var ev = new V1Event(
            //                    involvedObject: fooRef, 
            //                    metadata: new V1ObjectMeta(generateName: "StatusChangeEvent-", namespaceProperty: "default", ownerReferences: new List<V1OwnerReference> { fooOwnerRef }), 
            //                    related: fooRef, 
            //                    action: "StatusChanged", 
            //                    message: "Status changed to " + status, 
            //                    type: "StatusChange", 
            //                    reason: "Because...", 
            //                    lastTimestamp: DateTime.Now
            //                );
            //await _kubernetes.CreateNamespacedEventAsync(ev, "default");
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
