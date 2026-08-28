using System;

[AttributeUsage(
    AttributeTargets.Class,
    AllowMultiple = false,
    Inherited = false)]
public sealed class PoiChallengeTypeAttribute : Attribute
{
    public string TypeId { get; }

    public PoiChallengeTypeAttribute(string typeId)
    {
        if (string.IsNullOrWhiteSpace(typeId))
            throw new ArgumentException(
                "Challenge type ID cannot be empty.",
                nameof(typeId)
            );

        TypeId = typeId;
    }
}