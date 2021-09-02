// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Ngsa.BurstService
{
    public class Config
    {
        public string SecretsVolume { get; set; } = "secrets";
        public LogLevel LogLevel { get; set; } = LogLevel.Warning;
        public bool IsLogLevelSet { get; set; }
        public Secrets Secrets { get; set; }
        public bool DryRun { get; set; }
        public bool Prometheus { get; set; }
        public string Zone { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int Port { get; set; } = 8080;
        public int Frequency { get; set; } = 5;

        public void SetConfig(Config config)
        {
            DryRun = config.DryRun;
            Frequency = config.Frequency;
            IsLogLevelSet = config.IsLogLevelSet;
            Port = config.Port;
            Prometheus = config.Prometheus;
            Secrets = config.Secrets;

            // LogLevel.Information is the min
            LogLevel = config.LogLevel <= LogLevel.Information ? LogLevel.Information : config.LogLevel;

            // clean up string values
            SecretsVolume = string.IsNullOrWhiteSpace(config.SecretsVolume) ? string.Empty : config.SecretsVolume.Trim();
            Region = string.IsNullOrWhiteSpace(config.Region) ? string.Empty : config.Region.Trim();
            Zone = string.IsNullOrWhiteSpace(config.Zone) ? string.Empty : config.Zone.Trim();
        }
    }
}
