using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Stores the project-specific map image and the normalized positions
/// of the POIs displayed on it.
/// </summary>
[CreateAssetMenu(
    fileName = "MapDefinition",
    menuName = "THEODEN/Map Definition"
)]
public class MapDefinition : ScriptableObject
{
    [Header("Map")]
    [SerializeField]
    private Sprite mapImage;

    [Header("Pins")]
    [SerializeField]
    private List<MapPinDefinition> pins = new();

    public Sprite MapImage => mapImage;
    public IReadOnlyList<MapPinDefinition> Pins => pins;

    public void SetMapImage(Sprite newMapImage)
    {
        mapImage = newMapImage;
    }

    public bool ContainsPoi(string poiId)
    {
        return !string.IsNullOrWhiteSpace(poiId) &&
               pins.Any(pin => pin.PoiId == poiId);
    }

    public bool AddPin(string poiId, Vector2 normalizedPosition)
    {
        if (string.IsNullOrWhiteSpace(poiId) || ContainsPoi(poiId))
        {
            return false;
        }

        pins.Add(
            new MapPinDefinition(
                poiId,
                ClampNormalizedPosition(normalizedPosition)
            )
        );
        return true;
    }

    public bool UpdatePinPosition(
        string poiId,
        Vector2 normalizedPosition
    )
    {
        MapPinDefinition pin =
            pins.Find(candidate => candidate.PoiId == poiId);

        if (pin == null)
        {
            return false;
        }

        pin.SetNormalizedPosition(
            ClampNormalizedPosition(normalizedPosition)
        );

        return true;
    }

    public bool RemovePin(string poiId)
    {
        MapPinDefinition pin =
            pins.Find(candidate => candidate.PoiId == poiId);

        return pin != null && pins.Remove(pin);
    }

    private void OnValidate()
    {
        foreach (MapPinDefinition pin in pins)
        {
            pin.ClampPosition();
        }
    }

    private static Vector2 ClampNormalizedPosition(Vector2 position)
    {
        return new Vector2(
            Mathf.Clamp01(position.x),
            Mathf.Clamp01(position.y)
        );
    }
}

/// <summary>
/// Associates a POI with a position relative to the map dimensions.
/// Coordinates range from 0 to 1.
/// (0, 0) is the top-left corner.
/// (1, 1) is the bottom-right corner.
/// </summary>
[Serializable]
public class MapPinDefinition
{
    [SerializeField]
    private string poiId;

    [SerializeField]
    private Vector2 normalizedPosition = new(0.5f, 0.5f);

    public string PoiId => poiId;
    public Vector2 NormalizedPosition => normalizedPosition;

    public MapPinDefinition(
        string poiId,
        Vector2 normalizedPosition
    )
    {
        this.poiId = poiId;
        this.normalizedPosition = normalizedPosition;
    }

    public void SetNormalizedPosition(Vector2 position)
    {
        normalizedPosition = position;
        ClampPosition();
    }

    public void ClampPosition()
    {
        normalizedPosition = new Vector2(
            Mathf.Clamp01(normalizedPosition.x),
            Mathf.Clamp01(normalizedPosition.y)
        );
    }
}