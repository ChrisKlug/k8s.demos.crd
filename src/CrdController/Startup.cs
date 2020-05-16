using k8s;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

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
            }
            else
            {
                config = new KubernetesClientConfiguration { Host = "http://127.0.0.1:8001" };
            }

            services.AddHttpClient("K8s")
                    .AddTypedClient<IKubernetes>((httpClient, serviceProvider) => new Kubernetes(config, httpClient))
                    .ConfigurePrimaryHttpMessageHandler(config.CreateDefaultHttpClientHandler)
                    .AddHttpMessageHandler(KubernetesClientConfiguration.CreateWatchHandler);

            services.AddSingleton<IFooList, FooList>();
            services.AddHostedService<CrdControllerService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IFooList foos)
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
                    await context.Response.WriteAsync(string.Join("\r\n", foos.Foos.Select(x => x.Metadata.Name)));
                });
            });
        }
    }
}
