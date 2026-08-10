using System;
using UnityEngine;

namespace Config
{
    /// <summary>
    /// Provides access to the active THEODEN runtime configuration.
    /// </summary>
    public static class TheodenRuntimeConfigProvider
    {
        private const string ResourcesPath =
            "THEODEN/TheodenRuntimeConfig";

        private static TheodenRuntimeConfig cachedConfig;

        /// <summary>
        /// Returns the project identifier of the active application.
        /// </summary>
        public static string ProjectId
        {
            get
            {
                TheodenRuntimeConfig config = GetConfig();

                if (config == null)
                {
                    throw new InvalidOperationException(
                        "THEODEN runtime configuration was not found " +
                        $"at Resources/{ResourcesPath}."
                    );
                }

                if (string.IsNullOrWhiteSpace(config.ProjectId))
                {
                    throw new InvalidOperationException(
                        "The THEODEN runtime project identifier is empty."
                    );
                }

                return config.ProjectId;
            }
        }

        private static TheodenRuntimeConfig GetConfig()
        {
            if (cachedConfig == null)
            {
                cachedConfig =
                    Resources.Load<TheodenRuntimeConfig>(
                        ResourcesPath
                    );
            }

            return cachedConfig;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            cachedConfig = null;
        }
    }
}