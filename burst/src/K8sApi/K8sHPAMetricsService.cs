// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ngsa.BurstService.K8sApi
{
    public class K8sHPAMetricsService : IHostedService, IDisposable, IK8sHPAMetricsService
    {
        private readonly ILogger<K8sHPAMetricsService> logger;
        private readonly IKubernetes client;
        private Timer timer;
        private V2beta2HorizontalPodAutoscalerList hpaList;

        public K8sHPAMetricsService(ILogger<K8sHPAMetricsService> logger)
        {
            this.logger = logger;

            KubernetesClientConfiguration config;

            // Create a new config and a client
            if (KubernetesClientConfiguration.IsInCluster())
            {
                // Get In-Cluster Config
                config = KubernetesClientConfiguration.InClusterConfig();
            }
            else
            {
                // Otherwise get the default config from K8s
                config = KubernetesClientConfiguration.BuildDefaultConfig();
            }

            // Create a k8s client from config
            client = new Kubernetes(config);
        }

        public K8sHPAMetrics GetK8SHPAMetrics(string ns, string deployment)
        {
            if (hpaList == null)
            {
                logger.LogWarning("HPA List is not populated");
                return null;
            }

            K8sHPAMetrics hpaMetrics = new ();

            // If _hpaList is not null, we don't have any HPA
            if (hpaList.Items.Count == 0)
            {
                logger.LogWarning("No HPA object found in any namespace");
            }
            else
            {
                foreach (V2beta2HorizontalPodAutoscaler hpa in hpaList.Items)
                {
                    if (hpa.Namespace().Equals(ns) && hpa.Name().Equals(deployment))
                    {
                        // Get the Target CPU load
                        hpaMetrics.TargetCPULoad = GetTargetCpuLoad(hpa);

                        // Get the current CPU load
                        hpaMetrics.CurrentCPULoad = GetCurrentCpuLoad(hpa);
                        return hpaMetrics;
                    }
                }
            }

            // At the very least return empty metrics
            return hpaMetrics;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(TimerWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(App.Config.Frequency));
            logger.LogInformation("Running timed k8s HPA-API Service...");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Stopping k8s HPA-API Service...");
            timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (timer != null)
                {
                    timer.Dispose();
                }
            }
        }

        private void TimerWork(object state)
        {
            // Call the K8s API
            // TODO: Try out differt version of API
            try
            {
                V2beta2HorizontalPodAutoscalerList hpaList = client.ListHorizontalPodAutoscalerForAllNamespaces2(timeoutSeconds: 1);

                if (this.hpaList == null)
                {
                    this.hpaList = hpaList;
                }
                else
                {
                    // TODO: Might be unncessary to use InterLocking
                    Interlocked.Exchange<V2beta2HorizontalPodAutoscalerList>(ref this.hpaList, hpaList);
                }
            }
            catch (Exception ex)
            {
                // Don't have any HPA API Enabled!!
                logger.LogError(ex, "Failed to get HPA objects. Check ClusterRole or HPA objects");
            }
        }

        private int? GetCurrentCpuLoad(V2beta2HorizontalPodAutoscaler hpa)
        {
            // Check if we created HPA but but don't have a metrics server
            if (hpa?.Status?.CurrentMetrics != null)
            {
                foreach (V2beta2MetricStatus m in hpa.Status.CurrentMetrics)
                {
                    // We're interested in CPU metrics
                    if (m.Resource.Name == "cpu")
                    {
                        return m.Resource.Current.AverageUtilization;
                    }
                }
            }

            logger.LogWarning("Cannot get HPA metrics (probable cause: no metrics server)");

            return null;
        }

        private int? GetTargetCpuLoad(V2beta2HorizontalPodAutoscaler hpa)
        {
            // Check if we created HPA but didn't set any CPU Target
            if (hpa?.Spec?.Metrics != null)
            {
                foreach (V2beta2MetricSpec m in hpa.Spec.Metrics)
                {
                    // We're interested in CPU metrics
                    if (m.Resource.Name == "cpu")
                    {
                        return m.Resource.Target.AverageUtilization;
                    }
                }
            }

            logger.LogWarning("HPA Spec is not set");
            return null;
        }
    }
}
