// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Ngsa.BurstService.K8sApi;

namespace Ngsa.BurstService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BurstMetricsController : ControllerBase
    {
        //private TimedHostedService timedService
        private readonly ILogger<BurstMetricsController> logger;
        private readonly IK8sHPAMetricsService service;
        public BurstMetricsController(ILogger<BurstMetricsController> logger, IK8sHPAMetricsService timedService)
        {
            this.logger = logger;
            if (timedService != null)
            {
                // _logger.LogInformation("Got it right");
                service = timedService;
            }
            else
            {
                // _logger.LogInformation("Somrhing is wrong");
            }
        }

        [HttpGet("{ns}/{deployment}")]
        public IActionResult Get(string ns, string deployment)
        {
            K8sHPAMetrics hpaMetrics = service.GetK8SHPAMetrics(ns, deployment);

            // Nullable interpolation will return "" for null objects
            // string cpuTarget = $"{hpaMetrics?.TargetCPULoad}";
            // string cpuCurrent = $"{hpaMetrics?.CurrentCPULoad}";
            // But we can control what to output if we do null
            // TODO: Set the default value from appsettings.json

            string cpuTarget = hpaMetrics?.TargetCPULoad?.ToString() ?? "-1";
            string cpuCurrent = hpaMetrics?.CurrentCPULoad?.ToString() ?? "-1";

            // Get the CPU Target
            logger.LogDebug("Target: {}, Cur CPU: {}", cpuTarget, cpuCurrent);

            // Console.WriteLine($"{DateTime.Now:s}  {Request.Path.ToString()}");
            return Ok($"service={ns}/{deployment}, current-load={cpuCurrent}, target-load={cpuTarget}, max-load=85");
        }
    }
}
