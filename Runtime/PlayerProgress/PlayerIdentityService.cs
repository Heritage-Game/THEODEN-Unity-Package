using System;
using UnityEngine;

/// <summary>
/// Provides a persistent identifier for the current player.
/// </summary>
public static class PlayerIdentityService
{
    private const string PlayerIdPlayerPrefsKey =
        "THEODEN_PLAYER_ID";

    /// <summary>
    /// Returns the existing player identifier or creates
    /// a new persistent one.
    /// </summary>
    public static string GetOrCreatePlayerId()
    {
        string existingPlayerId =
            PlayerPrefs.GetString(
                PlayerIdPlayerPrefsKey,
                string.Empty
            );

        if (!string.IsNullOrWhiteSpace(existingPlayerId))
            return existingPlayerId;

        string newPlayerId =
            Guid.NewGuid().ToString("N");

        PlayerPrefs.SetString(
            PlayerIdPlayerPrefsKey,
            newPlayerId
        );

        PlayerPrefs.Save();

        Debug.Log(
            "[PlayerIdentityService] Created player ID: " +
            newPlayerId
        );

        return newPlayerId;
    }
}