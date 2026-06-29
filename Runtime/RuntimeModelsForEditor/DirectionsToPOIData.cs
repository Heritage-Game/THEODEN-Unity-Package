using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeModelsForEditor
{
    /// <summary>
    /// Editor-side data model used to export directions content for a Point of Interest.
    /// </summary>
    /// <remarks>
    /// This model is used during the editor export pipeline.
    /// It can contain direct Unity asset references such as <see cref="Sprite"/> and <see cref="AudioClip"/>.
    ///
    /// During JSON serialization, these Unity asset references are converted into Addressables
    /// address strings by <see cref="AddressableAssetJsonConverter"/>.
    ///
    /// This class is not intended to be the final runtime loading model.
    /// The runtime can deserialize the exported JSON into a separate DTO containing string addresses.
    /// </remarks>
    [Serializable]
    public class DirectionsToPOIData
    {
        /// <summary>
        /// Display name of the target Point of Interest.
        /// </summary>
        public string poiName;

        /// <summary>
        /// Unique identifier of the target Point of Interest.
        /// </summary>
        public string poiId;

        /// <summary>
        /// Textual directions that guide the player toward the Point of Interest.
        /// </summary>
        public string description;

        /// <summary>
        /// Optional images used to visually support the directions.
        /// </summary>
        /// <remarks>
        /// These sprites are editor-side references.
        /// In the exported JSON, they will be serialized as Addressables address strings.
        /// </remarks>
        public List<Sprite> images = new List<Sprite>();

        /// <summary>
        /// Optional audio description for the directions.
        /// </summary>
        /// <remarks>
        /// This audio clip is an editor-side reference.
        /// In the exported JSON, it will be serialized as an Addressables address string.
        /// </remarks>
        public AudioClip audioDescription;
    }
}