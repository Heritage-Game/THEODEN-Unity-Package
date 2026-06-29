using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Core.Models
{
    [Serializable]
    public class CodexModel
    {
        public string menuTitle;
        public List<CodexItemDefinition> items;
    }

    [Serializable]
    public class CodexItemDefinition
    {
        public string levelTitle;
        public string levelSubTitle;
        public MenuActionType actionType;
        public string target;
        [FormerlySerializedAs("poi_id")] public string poiId;
        public CodexItemState state;

    }
}