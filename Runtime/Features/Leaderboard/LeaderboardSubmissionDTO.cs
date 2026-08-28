using System;
using Newtonsoft.Json;

/// <summary>
/// Data sent to the leaderboard service.
/// </summary>
[Serializable]
public class LeaderboardSubmissionDTO
{
    [JsonProperty("project_id")]
    public string ProjectId { get; set; }

    [JsonProperty("player_id")]
    public string PlayerId { get; set; }

    [JsonProperty("nickname")]
    public string Nickname { get; set; }

    [JsonProperty("score")]
    public int Score { get; set; }

    [JsonProperty("total_time")]
    public int TotalTime { get; set; }
}

/// <summary>
/// One entry returned by the leaderboard service.
/// </summary>
[Serializable]
public sealed class LeaderboardEntryDTO :
    LeaderboardSubmissionDTO
{
}