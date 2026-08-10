/// <summary>
/// Defines the scoring rules shared by THEODEN authoring tools and runtime logic.
/// </summary>
public static class TheodenScoringRules
{
    /// <summary>
    /// Points awarded when a challenge does not define a valid custom value.
    /// </summary>
    public const int DefaultChallengePoints = 100;

    /// <summary>
    /// Returns the configured points when valid, otherwise the default value.
    /// </summary>
    public static int ResolveChallengePoints(int configuredPoints)
    {
        return configuredPoints > 0
            ? configuredPoints
            : DefaultChallengePoints;
    }
}