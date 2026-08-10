using UnityEngine;

namespace Config
{
    /// <summary>
    /// Contains the minimal project information required by the
    /// THEODEN runtime.
    /// </summary>
    public sealed class TheodenRuntimeConfig : ScriptableObject
    {
        [SerializeField]
        private string projectId;

        [SerializeField]
        [Min(0)]
        private int totalPoiCount;

        [SerializeField]
        private string leaderboardBaseUrl;

        /// <summary>
        /// Stable identifier used to namespace this application.
        /// </summary>
        public string ProjectId => projectId;

        /// <summary>
        /// Total number of POIs included in the application.
        /// </summary>
        public int TotalPoiCount => totalPoiCount;

        /// <summary>
        /// Base URL of the leaderboard service.
        /// </summary>
        public string LeaderboardBaseUrl =>
            leaderboardBaseUrl?.TrimEnd('/');

        /// <summary>
        /// Returns whether a leaderboard service was configured.
        /// </summary>
        public bool HasLeaderboardConfiguration =>
            !string.IsNullOrWhiteSpace(leaderboardBaseUrl);
    }
}