namespace FuzzPhyte.Placement
{
    using UnityEngine;
    using System.Collections.Generic;
    public enum MeshVertexPickMode
    {
        EvenInOrder,
        Random
    }
    public static class MeshVertexPlacer
    {
        public static void ApplyToTransforms(
               IReadOnlyList<Transform> items,
               IReadOnlyList<Vector3> worldVertices,
               MeshVertexPickMode mode,
               bool orientToNormal = false,
               IReadOnlyList<Vector3> worldNormals = null,
               int randomSeed = 0
           )
        {
            if (items == null || worldVertices == null) return;
            if (items.Count == 0 || worldVertices.Count == 0) return;

            var rng = (randomSeed == 0) ? new System.Random() : new System.Random(randomSeed);

            for (int i = 0; i < items.Count; i++)
            {
                int vi = mode == MeshVertexPickMode.Random
                    ? rng.Next(0, worldVertices.Count)
                    : (i % worldVertices.Count);

                var t = items[i];
                if (t == null) continue;

                t.position = worldVertices[vi];

                // Optional: if you later want orientation by normal (requires normals gathered + transformed)
                if (orientToNormal && worldNormals != null && worldNormals.Count == worldVertices.Count)
                {
                    var n = worldNormals[vi];
                    if (n.sqrMagnitude > 0.000001f)
                        t.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(t.forward, n).normalized, n);
                }
            }
        }
    }
}
