using System;
using System.Collections.Generic;

/// <summary>
/// Persistent progression data of the current player.
/// </summary>
[Serializable]
public class PlayerProgressData
{
    /// <summary>
    /// Version of the saved progress structure.
    /// </summary>
    public int schemaVersion = 1;

    /// <summary>
    /// Total points earned by the player.
    /// </summary>
    public int totalPoints;

    /// <summary>
    /// IDs of the POIs whose challenges have already been completed.
    /// </summary>
    public List<PoiProgressData> completedPois = new List<PoiProgressData>();
}       