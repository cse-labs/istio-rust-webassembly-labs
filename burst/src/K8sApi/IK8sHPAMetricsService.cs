// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Ngsa.BurstService.K8sApi
{
    public interface IK8sHPAMetricsService
    {
        K8sHPAMetrics GetK8SHPAMetrics(string ns, string deployment);
    }
}
