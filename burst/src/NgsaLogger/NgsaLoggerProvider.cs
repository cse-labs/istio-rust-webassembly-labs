// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Ngsa.Middleware
{
    /// <summary>
    /// NGSA Logger Provider
    /// </summary>
    public sealed class NgsaLoggerProvider : ILoggerProvider
    {
        private readonly NgsaLoggerConfiguration config;
        private readonly ConcurrentDictionary<string, NgsaLogger> loggers = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="NgsaLoggerProvider"/> class.
        /// </summary>
        /// <param name="loggerConfig">NgsaLoggerConfig</param>
        public NgsaLoggerProvider(NgsaLoggerConfiguration loggerConfig)
        {
            config = loggerConfig;
        }

        /// <summary>
        /// Create a logger by category name (usually assembly)
        /// </summary>
        /// <param name="categoryName">Category Name</param>
        /// <returns>ILogger</returns>
        public ILogger CreateLogger(string categoryName)
        {
            NgsaLogger logger = loggers.GetOrAdd(categoryName, new NgsaLogger(categoryName, config));
            return logger;
        }

        /// <summary>
        /// IDispose.Dispose()
        /// </summary>
        public void Dispose()
        {
            loggers.Clear();
        }
    }
}
