﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Ngsa.Middleware
{
    /// <summary>
    /// NGSA Logger Configuration
    /// </summary>
    public class NgsaLoggerConfiguration
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Warning;
    }
}
