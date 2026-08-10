/// <summary>
/// Contains the result of a completed challenge session.
/// </summary>
public sealed class ChallengeSessionResult
{
    public string PoiId { get; }

    public string SubmittedAnswer { get; }

    public int AttemptsUsed { get; }

    public bool SolvedByPlayer { get; }

    public float CompletionTimeSeconds { get; }

    internal ChallengeSessionResult(
        string poiId,
        string submittedAnswer,
        int attemptsUsed,
        bool solvedByPlayer,
        float completionTimeSeconds)
    {
        PoiId = poiId;
        SubmittedAnswer = submittedAnswer;
        AttemptsUsed = attemptsUsed;
        SolvedByPlayer = solvedByPlayer;
        CompletionTimeSeconds = completionTimeSeconds;
    }
}