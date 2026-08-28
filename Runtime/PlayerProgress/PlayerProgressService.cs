using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Manages the persistent progression of the current player.
/// </summary>
public static class PlayerProgressService
{
    private const string ProgressPlayerPrefsKey =
        "THEODEN_PLAYER_PROGRESS";

    private static PlayerProgressData cachedProgress;

    /// <summary>
    /// Total points earned by the player.
    /// </summary>
    public static int TotalPoints =>
        GetProgress().totalPoints;

    /// <summary>
    /// Total time spent completing all registered POIs.
    /// </summary>
    public static float TotalCompletionTimeSeconds
    {
        get
        {
            float totalTime = 0f;

            foreach (PoiProgressData poiProgress
                     in GetProgress().completedPois)
            {
                if (poiProgress == null)
                    continue;

                totalTime += Mathf.Max(
                    0f,
                    poiProgress.completionTimeSeconds
                );
            }

            return totalTime;
        }
    }
    
    /// <summary>
    /// Returns whether the specified POI has already been completed.
    /// </summary>
    public static bool IsPoiCompleted(string poiId)
    {
        return TryGetPoiProgress(
            poiId,
            out _  //discard - we only need the boolean value
        );
    }

    /// <summary>
    /// Returns the saved completion data for a POI.
    /// </summary>
    public static bool TryGetPoiProgress(
        string poiId,
        out PoiProgressData poiProgress)
    {
        poiProgress = null;

        if (string.IsNullOrWhiteSpace(poiId))
            return false;

        poiProgress = GetProgress().completedPois.Find(
            savedPoi =>
                savedPoi != null &&
                string.Equals(
                    savedPoi.poiId,
                    poiId,
                    StringComparison.Ordinal
                )
        );

        return poiProgress != null;
    }

    /// <summary>
    /// Registers the completion of a POI.
    /// A POI can be registered only once.
    /// </summary>
    /// <returns>
    /// True if a new completion was registered.
    /// False if the POI was invalid or already completed.
    /// </returns>
    public static bool TryRegisterPoiCompletion(
        POIModel poi,
        string submittedAnswer,
        int attemptsUsed,
        bool solvedByPlayer,
        float completionTimeSeconds,
        out PoiProgressData savedPoiProgress)
    {
        savedPoiProgress = null;

        if (poi == null)
        {
            Debug.LogError(
                "[PlayerProgressService] Cannot complete a null POI."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(poi.poiId))
        {
            Debug.LogError(
                "[PlayerProgressService] Cannot complete a POI " +
                "without a valid ID."
            );

            return false;
        }

        if (TryGetPoiProgress(
                poi.poiId,
                out PoiProgressData existingProgress))
        {
            poi.isChallengeCompleted = true;
            savedPoiProgress = existingProgress;

            Debug.Log(
                "[PlayerProgressService] POI already completed: " +
                poi.poiId
            );

            return false;
        }

        int configuredPoints =
            TheodenScoringRules.ResolveChallengePoints(
                poi.points
            );

        int awardedPoints =
            solvedByPlayer ? configuredPoints : 0;

        savedPoiProgress = new PoiProgressData
        {
            poiId = poi.poiId,
            awardedPoints = awardedPoints,

            submittedAnswer = submittedAnswer ?? "",
            attemptsUsed = Mathf.Max(1, attemptsUsed),
            solvedByPlayer = solvedByPlayer,

            completionTimeSeconds =
                Mathf.Max(0f, completionTimeSeconds),

            completedAtUtc =
                DateTime.UtcNow.ToString("O")
        };

        PlayerProgressData progress = GetProgress();

        progress.completedPois.Add(savedPoiProgress);
        progress.totalPoints += awardedPoints;

        poi.isChallengeCompleted = true;

        SaveProgress();

        Debug.Log(
            "[PlayerProgressService] Completed POI: " +
            poi.poiId +
            " | Awarded points: " +
            awardedPoints +
            " | Time: " +
            savedPoiProgress.completionTimeSeconds +
            " seconds | Total points: " +
            progress.totalPoints
        );

        return true;
    }

    /// <summary>
    /// Synchronizes the runtime completion state of a POI.
    /// </summary>
    public static void ApplyCompletionState(POIModel poi)
    {
        if (poi == null)
            return;

        poi.isChallengeCompleted =
            IsPoiCompleted(poi.poiId);
    }

    /// <summary>
    /// Returns the IDs of all completed POIs.
    /// </summary>
    public static IReadOnlyList<string> GetCompletedPoiIds()
    {
        List<string> completedPoiIds =
            new List<string>();

        foreach (PoiProgressData poiProgress
                 in GetProgress().completedPois)
        {
            if (poiProgress == null)
                continue;

            if (string.IsNullOrWhiteSpace(poiProgress.poiId))
                continue;

            completedPoiIds.Add(poiProgress.poiId);
        }

        return completedPoiIds;
    }

    /// <summary>
    /// Returns the number of completed POIs.
    /// </summary>
    public static int GetCompletedPoiCount()
    {
        return GetProgress().completedPois.Count;
    }

    /// <summary>
    /// Returns the POI completion ratio between 0 and 1.
    /// </summary>
    public static float GetPoiCompletionRatio(
        int totalPoiCount)
    {
        if (totalPoiCount <= 0)
            return 0f;

        return Mathf.Clamp01(
            (float)GetCompletedPoiCount() /
            totalPoiCount
        );
    }

    /// <summary>
    /// Returns the total time spent completing all saved POIs.
    /// </summary>
    public static float GetTotalCompletionTimeSeconds()
    {
        float totalTimeSeconds = 0f;

        foreach (PoiProgressData poiProgress
                 in GetProgress().completedPois)
        {
            if (poiProgress == null)
                continue;

            totalTimeSeconds += Mathf.Max(
                0f,
                poiProgress.completionTimeSeconds
            );
        }

        return totalTimeSeconds;
    }
    
    /// <summary>
    /// Deletes all progression data.
    /// </summary>
    public static void ResetProgress()
    {
        cachedProgress = new PlayerProgressData();

        PlayerPrefs.DeleteKey(ProgressPlayerPrefsKey);
        PlayerPrefs.Save();

        Debug.Log(
            "[PlayerProgressService] Player progress reset."
        );
    }

    private static PlayerProgressData GetProgress()
    {
        if (cachedProgress == null)
            cachedProgress = LoadProgress();

        return cachedProgress;
    }

    private static PlayerProgressData LoadProgress()
    {
        string json = PlayerPrefs.GetString(
            ProgressPlayerPrefsKey,
            string.Empty
        );

        if (string.IsNullOrWhiteSpace(json))
            return new PlayerProgressData();

        try
        {
            PlayerProgressData progress =
                JsonConvert.DeserializeObject<PlayerProgressData>(
                    json
                );

            if (progress == null)
                return new PlayerProgressData();

            progress.completedPois ??=
                new List<PoiProgressData>();

            if (progress.totalPoints < 0)
                progress.totalPoints = 0;

            return progress;
        }
        catch (JsonException exception)
        {
            Debug.LogError(
                "[PlayerProgressService] Could not load progress: " +
                exception.Message
            );

            return new PlayerProgressData();
        }
    }

    private static void SaveProgress()
    {
        string json = JsonConvert.SerializeObject(
            GetProgress()
        );

        PlayerPrefs.SetString(
            ProgressPlayerPrefsKey,
            json
        );

        PlayerPrefs.Save();
    }
}