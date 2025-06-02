namespace FuzzPhyte.Placement
{
    using UnityEngine;
    using System.Collections.Generic;

    [System.Serializable]
    public class VolumeMeta
    {
        public string VolumeName;
        public Bounds Bounds;
        public List<PlacementCategory> AllowedCategories = new();
        public List<PlacementCategory> DisallowedCategories = new();
        public List<PlacementCategory> DiscouragedCategories = new();
        public Dictionary<PlacementCategory, float> PriorityWeights = new();
        public float MaxWeight = 100f;
        public int MaxItems = 10;
        public bool StackingAllowed = true;
        public List<string> ThemeTags = new();
    }
}
