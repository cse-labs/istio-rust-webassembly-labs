// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ngsa.BurstService.K8sApi;
using Ngsa.Middleware;
using Prometheus;

namespace Ngsa.BurstService
{
    /// <summary>
    /// WebHostBuilder Startup
    /// </summary>
    public class Startup
    {
        private const string SwaggerTitle = "Next Gen Symmetric Apps";
#pragma warning disable IDE0044 // Add readonly modifier - in implementation, you may want to add a url prefix
        private static string swaggerPath = "/swagger.json";
#pragma warning restore IDE0044 // Add readonly modifier

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">the configuration for WebHost</param>
        public Startup(IConfiguration configuration)
        {
            // keep a local reference
            Configuration = configuration;
        }

        /// <summary>
        /// Gets IConfiguration
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configure the application builder
        /// </summary>
        /// <param name="app">IApplicationBuilder</param>
        /// <param name="env">IWebHostEnvironment</param>
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            // log http responses to the console
            // this should be first as it "wraps" all requests
            if (App.Config.LogLevel != LogLevel.None)
            {
                app.UseRequestLogger(new RequestLoggerOptions
                {
                    Log2xx = App.Config.LogLevel <= LogLevel.Information,
                    Log3xx = App.Config.LogLevel <= LogLevel.Information,
                    Log4xx = App.Config.LogLevel <= LogLevel.Warning,
                    Log5xx = true,
                });
            }

            // UseHsts in prod
            if (env.IsProduction())
            {
                app.UseHsts();
            }

            // redirect /
            app.Use(async (context, next) =>
            {
                // matches /
                if (context.Request.Path.Equals("/"))
                {
                    // return the version info
                    context.Response.Redirect("/index.html", true);
                    return;
                }
                else
                {
                    // not a match, so call next middleware handler
                    await next().ConfigureAwait(false);
                }
            });

            // add middleware handlers
            app.UseRouting()
                .UseEndpoints(ep =>
                {
                    ep.MapControllers();

                    if (App.Config.Prometheus)
                    {
                        ep.MapMetrics();
                    }
                })
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint(swaggerPath, SwaggerTitle);
                    c.RoutePrefix = string.Empty;
                })
                .UseVersion();
        }

        /// <summary>
        /// Service configuration
        /// </summary>
        /// <param name="services">The services in the web host</param>
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IK8sHPAMetricsService, K8sHPAMetricsService>();

            // Since we already added service, we will not add it again.
            // Rather we'll get it from service collection (provider).
            services.AddHostedService<K8sHPAMetricsService>(provider => provider.GetService<IK8sHPAMetricsService>() as K8sHPAMetricsService);

            // set json serialization defaults and api behavior
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
        }
    }
}
