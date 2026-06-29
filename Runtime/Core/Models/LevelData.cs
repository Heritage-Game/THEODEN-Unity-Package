using System;
using System.Collections.Generic;

namespace Core.Models
{
    [Serializable]
    public class CodexDataRoot
    {
        public List<LevelData> levels;
    }

    [Serializable]
    public class LevelData
    {
        public string id;
        public string title;
        public string subtitle;
        public string state;
        public string directions;
        public string description;
        public string qrCode;
        //public string poiImagePath;
        public List<string> poiImages;
        public string hint;
        public string badge;
        public string story;
        public ChallengeData challenge;

        public bool IsUnlocked()
        {
            return state == "unlocked";
        }
    }

    [Serializable]
    public class ChallengeData
    {
        public string question;
        public List<string> options;
        public int correctIndex;
        public string explanation;
    }
}