namespace FuzzPhyte.Placement
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;

    public class PlacementManager : MonoBehaviour
    {
        public List<PlacementRecord> AllPlacements = new();
        public float TotalScore => AllPlacements.Sum(r => r.Score);
        public void RegisterPlacement(PlacementRecord record)
        {
            AllPlacements.Add(record);
        }
    }
}
