namespace FuzzPhyte.Placement
{
    using UnityEngine;
    using System.Collections.Generic;

    public class PlacementRecord
    {
        public string ObjectID;
        public string VolumeID;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Score;
        public Dictionary<string, float> Metrics = new();

        public string StackedOnObjectID; // Who are we stacked on (null if not stacked)
        public List<string> StackedObjectsIDs = new(); // Who is stacked on us
    }
}
