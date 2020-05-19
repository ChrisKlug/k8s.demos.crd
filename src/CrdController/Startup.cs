using CrdController.Models;
using CrdController.HostedServices;
using CrdController.Utils;
using k8s;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using CrdController.Services;

namespace CrdController
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            KubernetesClientConfiguration config;
            if (KubernetesClientConfiguration.IsInCluster())
            {
                config = KubernetesClientConfiguration.InClusterConfig();
                services.AddSingleton<ILeaderSelector, KubernetesLeaderSelector>();
            }
            else
            {
                config = new KubernetesClientConfiguration { Host = "http://localhost:8001" };
                services.AddSingleton<ILeaderSelector, DummyLeaderSelector>();
            }

            services.AddHttpClient("K8s")
                    .AddTypedClient<IKubernetes>((httpClient, serviceProvider) => new Kubernetes(config, httpClient))
                    .ConfigurePrimaryHttpMessageHandler(config.CreateDefaultHttpClientHandler)
                    .AddHttpMessageHandler(KubernetesClientConfiguration.CreateWatchHandler);

            services.AddSingleton<ResourceSet<Foo>>();
            services.AddHostedService<CrdControllerService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ResourceSet<Foo> foos)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    context.Response.ContentType = "text/text";
                    await context.Response.WriteAsync(string.Join("\r\n", foos.Items.Select(x => x.Metadata.Name)));
                });
            });
        }
    }
}
