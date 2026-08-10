using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Manages the temporary state of the current challenge session.
/// </summary>
/// <remarks>
/// The session begins when the correct QR code is scanned and ends
/// when the player solves the challenge or the correct answer is
/// revealed after the maximum number of attempts.
/// </remarks>
public static class ChallengeSessionService
{
    /// <summary>
    /// Maximum number of answers the player can submit:
    /// one initial attempt and one retry.
    /// </summary>
    public const int MaxAttempts = 2;

    private static readonly Stopwatch SessionTimer =
        new Stopwatch();

    private static string activePoiId;
    private static string lastSubmittedAnswer;
    private static int attemptsUsed;

    /// <summary>
    /// True when a challenge session is currently active.
    /// </summary>
    public static bool IsActive =>
        !string.IsNullOrWhiteSpace(activePoiId) &&
        SessionTimer.IsRunning;

    /// <summary>
    /// ID of the POI associated with the active session.
    /// </summary>
    public static string ActivePoiId => activePoiId;

    /// <summary>
    /// Number of answers submitted during the active session.
    /// </summary>
    public static int AttemptsUsed => attemptsUsed;

    /// <summary>
    /// Number of attempts still available.
    /// </summary>
    public static int RemainingAttempts =>
        Mathf.Max(0, MaxAttempts - attemptsUsed);

    /// <summary>
    /// True when the player can submit another answer.
    /// </summary>
    public static bool HasAttemptsRemaining =>
        attemptsUsed < MaxAttempts;

    /// <summary>
    /// Current elapsed session time in seconds.
    /// </summary>
    public static float ElapsedSeconds =>
        (float)SessionTimer.Elapsed.TotalSeconds;

    /// <summary>
    /// Starts a new challenge session.
    /// </summary>
    /// <returns>
    /// True if the session was started; false if the POI ID is invalid
    /// or the same session is already active.
    /// </returns>
    public static bool StartSession(string poiId)
    {
        if (string.IsNullOrWhiteSpace(poiId))
        {
            Debug.LogError(
                "[ChallengeSessionService] Cannot start a session " +
                "without a valid POI ID."
            );

            return false;
        }

        if (IsActive &&
            string.Equals(
                activePoiId,
                poiId,
                StringComparison.Ordinal))
        {
            Debug.LogWarning(
                "[ChallengeSessionService] A session for this POI " +
                "is already active: " +
                poiId
            );

            return false;
        }

        if (IsActive)
        {
            Debug.LogWarning(
                "[ChallengeSessionService] Replacing the active " +
                "session for " +
                activePoiId +
                " with a session for " +
                poiId
            );

            ClearSessionState();
        }

        activePoiId = poiId;
        lastSubmittedAnswer = "";
        attemptsUsed = 0;

        SessionTimer.Restart();

        Debug.Log(
            "[ChallengeSessionService] Session started for POI: " +
            poiId
        );

        return true;
    }

    /// <summary>
    /// Registers an answer submitted by the player.
    /// </summary>
    /// <returns>
    /// True if the attempt was registered; false if there is no active
    /// session, the answer is empty, or no attempts remain.
    /// </returns>
    public static bool RegisterAttempt(string submittedAnswer)
    {
        if (!IsActive)
        {
            Debug.LogError(
                "[ChallengeSessionService] Cannot register an " +
                "attempt because no session is active."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(submittedAnswer))
        {
            Debug.LogWarning(
                "[ChallengeSessionService] Empty answers are not " +
                "registered as attempts."
            );

            return false;
        }

        if (!HasAttemptsRemaining)
        {
            Debug.LogWarning(
                "[ChallengeSessionService] The maximum number of " +
                "attempts has already been reached."
            );

            return false;
        }

        attemptsUsed++;
        lastSubmittedAnswer = submittedAnswer;

        Debug.Log(
            "[ChallengeSessionService] Attempt registered: " +
            attemptsUsed +
            "/" +
            MaxAttempts
        );

        return true;
    }

    /// <summary>
    /// Completes the active challenge session.
    /// </summary>
    /// <param name="poiId">
    /// Expected POI ID. It must match the active session.
    /// </param>
    /// <param name="solvedByPlayer">
    /// True if the player selected the correct answer; false if the
    /// system revealed it after the final failed attempt.
    /// </param>
    /// <param name="result">
    /// Result containing answer, attempts and completion time.
    /// </param>
    public static bool TryCompleteSession(
        string poiId,
        bool solvedByPlayer,
        out ChallengeSessionResult result)
    {
        result = null;

        if (!IsActive)
        {
            Debug.LogError(
                "[ChallengeSessionService] Cannot complete the " +
                "session because no session is active."
            );

            return false;
        }

        if (!string.Equals(
                activePoiId,
                poiId,
                StringComparison.Ordinal))
        {
            Debug.LogError(
                "[ChallengeSessionService] POI mismatch. Active: " +
                activePoiId +
                " | Received: " +
                poiId
            );

            return false;
        }

        if (attemptsUsed <= 0)
        {
            Debug.LogError(
                "[ChallengeSessionService] Cannot complete a " +
                "session without a registered attempt."
            );

            return false;
        }

        SessionTimer.Stop();

        result = new ChallengeSessionResult(
            activePoiId,
            lastSubmittedAnswer,
            attemptsUsed,
            solvedByPlayer,
            (float)SessionTimer.Elapsed.TotalSeconds
        );

        Debug.Log(
            "[ChallengeSessionService] Session completed for POI: " +
            result.PoiId +
            " | Attempts: " +
            result.AttemptsUsed +
            " | Time: " +
            result.CompletionTimeSeconds +
            " seconds | Solved by player: " +
            result.SolvedByPlayer
        );

        ClearSessionState();

        return true;
    }

    /// <summary>
    /// Cancels the current session without saving a result.
    /// </summary>
    public static void CancelSession()
    {
        if (!IsActive)
            return;

        Debug.Log(
            "[ChallengeSessionService] Session cancelled for POI: " +
            activePoiId
        );

        ClearSessionState();
    }

    private static void ClearSessionState()
    {
        SessionTimer.Reset();

        activePoiId = null;
        lastSubmittedAnswer = "";
        attemptsUsed = 0;
    }
}