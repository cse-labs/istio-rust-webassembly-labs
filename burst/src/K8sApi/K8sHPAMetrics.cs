// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Ngsa.BurstService.K8sApi
{
    public class K8sHPAMetrics
    {
        public int? CurrentCPULoad { get; internal set; } = null;
        public int? TargetCPULoad { get; internal set; } = null;
    }
}
