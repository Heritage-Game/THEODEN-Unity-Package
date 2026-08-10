using System;

/// <summary>
/// Persistent result obtained from completing one POI.
/// </summary>
[Serializable]
public class PoiProgressData
{
    public string poiId;
    public int awardedPoints;

    // Used to recover information for visualizing the challenge after it's completed
    public string submittedAnswer;
    public int attemptsUsed;
    public bool solvedByPlayer;
    
    //Used to determine how much time the player takes to complete the challenge 
    public float completionTimeSeconds;
    public string completedAtUtc;
}