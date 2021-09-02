// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ngsa.BurstService
{
    public static class CosmosHealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddCosmosHealthCheck(
            this IHealthChecksBuilder builder,
            string name,
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null)
        {
            // Register a check of type Cosmos
            builder.AddCheck<CosmosHealthCheck>(name, failureStatus ?? HealthStatus.Degraded, tags);

            return builder;
        }
    }
}
