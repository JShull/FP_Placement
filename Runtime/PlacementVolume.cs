namespace FuzzPhyte.Placement
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;
    public class PlacementVolume
    {
        public Bounds Bounds;
        public VolumeMeta Meta;
        public bool IsOccupied;
        public List<PlacementVolume> SubVolumes = new();
        public List<PlacementRecord> PlacementRecords = new();

        public float TotalScore => PlacementRecords.Sum(r => r.Score);

        public void DrawGizmosRecursive(int depth = 0)
        {
            Color color = IsOccupied ? Color.red : Color.green;
            color.a = Mathf.Clamp01(0.3f + depth * 0.1f);
            Gizmos.color = color;
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(Bounds.center + Vector3.up * 0.05f, $"Depth {depth}");
#endif

            foreach (var sub in SubVolumes)
            {
                sub.DrawGizmosRecursive(depth + 1);
            }
        }
        public void Subdivide(Bounds placedObjectBounds)
        {
            // Calculate the remaining sub-volumes (simple AABB splitting)
            Vector3 min = Bounds.min;
            Vector3 max = Bounds.max;

            Vector3 objMin = placedObjectBounds.min;
            Vector3 objMax = placedObjectBounds.max;

            // Left volume (X-)
            if (objMin.x > min.x)
            {
                SubVolumes.Add(new PlacementVolume
                {
                    Bounds = new Bounds(
                        center: new Vector3((min.x + objMin.x) / 2f, Bounds.center.y, Bounds.center.z),
                        size: new Vector3(objMin.x - min.x, Bounds.size.y, Bounds.size.z)
                    ),
                    Meta = this.Meta
                });
            }

            // Right volume (X+)
            if (objMax.x < max.x)
            {
                SubVolumes.Add(new PlacementVolume
                {
                    Bounds = new Bounds(
                        center: new Vector3((objMax.x + max.x) / 2f, Bounds.center.y, Bounds.center.z),
                        size: new Vector3(max.x - objMax.x, Bounds.size.y, Bounds.size.z)
                    ),
                    Meta = this.Meta
                });
            }

            // Front volume (Z-)
            if (objMin.z > min.z)
            {
                SubVolumes.Add(new PlacementVolume
                {
                    Bounds = new Bounds(
                        center: new Vector3(Bounds.center.x, Bounds.center.y, (min.z + objMin.z) / 2f),
                        size: new Vector3(Bounds.size.x, Bounds.size.y, objMin.z - min.z)
                    ),
                    Meta = this.Meta
                });
            }

            // Back volume (Z+)
            if (objMax.z < max.z)
            {
                SubVolumes.Add(new PlacementVolume
                {
                    Bounds = new Bounds(
                        center: new Vector3(Bounds.center.x, Bounds.center.y, (objMax.z + max.z) / 2f),
                        size: new Vector3(Bounds.size.x, Bounds.size.y, max.z - objMax.z)
                    ),
                    Meta = this.Meta
                });
            }

            // Top volume (Y+), if stacking allowed
            if (Meta.StackingAllowed && objMax.y < max.y)
            {
                SubVolumes.Add(new PlacementVolume
                {
                    Bounds = new Bounds(
                        center: new Vector3(Bounds.center.x, (objMax.y + max.y) / 2f, Bounds.center.z),
                        size: new Vector3(Bounds.size.x, max.y - objMax.y, Bounds.size.z)
                    ),
                    Meta = this.Meta
                });
            }

            // Mark this volume as occupied
            IsOccupied = true;
        }

    }
}
