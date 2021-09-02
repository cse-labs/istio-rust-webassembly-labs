// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;

namespace Ngsa.BurstService
{
    /// <summary>
    /// Application secrets
    /// </summary>
    public class Secrets
    {
        public string Volume { get; set; }

        /// <summary>
        /// Get the secrets from the k8s volume
        /// </summary>
        /// <param name="volume">k8s volume name</param>
        /// <returns>Secrets or null</returns>
        public static Secrets GetSecretsFromVolume(string volume)
        {
            if (string.IsNullOrWhiteSpace(volume))
            {
                throw new ArgumentNullException(nameof(volume));
            }

            // thow exception if volume doesn't exist
            if (!Directory.Exists(volume))
            {
                throw new Exception($"Volume '{volume}' does not exist");
            }

            // get k8s secrets from volume
            Secrets sec = new ()
            {
                Volume = volume,
            };

            ValidateSecrets(volume, sec);

            return sec;
        }

        // basic validation of Cosmos values
        private static void ValidateSecrets(string volume, Secrets sec)
        {
            if (sec == null)
            {
                throw new Exception($"Unable to read secrets from volume: {volume}");
            }
        }

        // read a secret from a k8s volume
#pragma warning disable IDE0051 // Remove unused private members - this will be used in an implementation
        private static string GetSecretFromFile(string volume, string key)
#pragma warning restore IDE0051 // Remove unused private members
        {
            string val = string.Empty;

            if (File.Exists($"{volume}/{key}"))
            {
                val = File.ReadAllText($"{volume}/{key}").Trim();
            }

            return val;
        }
    }
}
