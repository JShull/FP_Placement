namespace FuzzPhyte.Placement
{
    using UnityEngine;
using FuzzPhyte.Utility; // Assuming FP_Data lives here

    [CreateAssetMenu(fileName = "PlacementCategory", menuName = "FuzzPhyte/Placement/Category", order = 10)]
    public class PlacementCategory : FP_Data
    {
        [TextArea]
        public string Description;
        public Sprite Icon; // Optional, for visual UI
    }
}
