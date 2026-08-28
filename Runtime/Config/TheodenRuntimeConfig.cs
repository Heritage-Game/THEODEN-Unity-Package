using UnityEngine;

namespace Config
{
    /// <summary>
    /// Contains the project information required by the
    /// THEODEN application at runtime.
    /// </summary>
    public sealed class TheodenRuntimeConfig : ScriptableObject
    {
        [SerializeField]
        private string projectId;

        [SerializeField]
        private bool useLeaderboard;

        [SerializeField]
        private string leaderboardBaseUrl;

        [SerializeField, Min(0)]
        private int totalPoiCount;

        /// <summary>
        /// Stable identifier of the current THEODEN project.
        /// </summary>
        public string ProjectId => projectId;

        /// <summary>
        /// Whether the leaderboard service is enabled.
        /// </summary>
        public bool UseLeaderboard => useLeaderboard;

        /// <summary>
        /// Base URL of the leaderboard API.
        /// </summary>
        public string LeaderboardBaseUrl =>
            string.IsNullOrWhiteSpace(leaderboardBaseUrl)
                ? string.Empty
                : leaderboardBaseUrl.Trim().TrimEnd('/');

        /// <summary>
        /// Total number of POIs available in the project.
        /// </summary>
        public int TotalPoiCount => totalPoiCount;

        /// <summary>
        /// Whether the runtime has all the information required
        /// to contact the leaderboard service.
        /// </summary>
        public bool HasLeaderboardConfiguration =>
            useLeaderboard &&
            !string.IsNullOrWhiteSpace(LeaderboardBaseUrl);
    }
}