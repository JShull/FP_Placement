namespace FuzzPhyte.Placement
{
    using UnityEngine;
    using System.Collections.Generic;
    public static class MeshVertexLayoutUtility
    {
        public static bool TryGetWorldVertices(
                GameObject meshSource,
                out List<Vector3> worldVertices,
                bool includeSkinned = true,
                bool removeDuplicates = false,
                float duplicateEpsilon = 0.0001f
            )
        {
            worldVertices = new List<Vector3>();
            if (meshSource == null) return false;

            // 1) MeshFilter path
            var mf = meshSource.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                AppendWorldVertices(mf.sharedMesh, mf.transform, worldVertices);
                if (removeDuplicates) DeduplicateInPlace(worldVertices, duplicateEpsilon);
                return worldVertices.Count > 0;
            }

            // 2) SkinnedMeshRenderer path (optional)
            if (includeSkinned)
            {
                var smr = meshSource.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    var baked = new Mesh();
                    smr.BakeMesh(baked);

                    // Baked vertices are in the renderer's local space, so TransformPoint works the same way.
                    AppendWorldVertices(baked, smr.transform, worldVertices);
                    if (removeDuplicates) DeduplicateInPlace(worldVertices, duplicateEpsilon);

                    Object.DestroyImmediate(baked);
                    return worldVertices.Count > 0;
                }
            }

            return false;
        }
        public static bool TryGetWorldVerticesAndNormals(
            GameObject meshSource,
            out List<Vector3> worldVertices,
            out List<Vector3> worldNormals,
            bool includeSkinned = true,
            bool removeDuplicates = false,
            float duplicateEpsilon = 0.0001f
        )
        {
            worldVertices = new List<Vector3>();
            worldNormals = new List<Vector3>();

            if (meshSource == null) return false;

            // 1) MeshFilter
            var mf = meshSource.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                AppendWorldVerticesAndNormals(mf.sharedMesh, mf.transform, worldVertices, worldNormals);

                if (removeDuplicates)
                    DeduplicateVertsAndNormalsInPlace(worldVertices, worldNormals, duplicateEpsilon);

                return worldVertices.Count > 0;
            }

            // 2) SkinnedMeshRenderer (optional)
            if (includeSkinned)
            {
                var smr = meshSource.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    var baked = new Mesh();
                    smr.BakeMesh(baked);

                    AppendWorldVerticesAndNormals(baked, smr.transform, worldVertices, worldNormals);

                    if (removeDuplicates)
                        DeduplicateVertsAndNormalsInPlace(worldVertices, worldNormals, duplicateEpsilon);

                    Object.DestroyImmediate(baked);
                    return worldVertices.Count > 0;
                }
            }

            return false;
        }

        /// <summary>
        /// Filters world vertices/normals to only those contained within the given BoxCollider volume.
        /// Works with rotated colliders by transforming points into collider local space.
        /// </summary>
        public static void FilterByBoxColliderInPlace(
            List<Vector3> worldVertices,
            List<Vector3> worldNormals,
            BoxCollider boxCollider,
            bool includeBoundary = true,
            bool invert = false
        )
        {
            if (boxCollider == null) return;
            if (worldVertices == null || worldVertices.Count == 0) return;

            bool hasNormals = worldNormals != null && worldNormals.Count == worldVertices.Count;

            Transform ct = boxCollider.transform;

            Vector3 half = boxCollider.size * 0.5f;
            Vector3 center = boxCollider.center;

            int write = 0;
            for (int i = 0; i < worldVertices.Count; i++)
            {
                Vector3 wp = worldVertices[i];
                Vector3 lp = ct.InverseTransformPoint(wp);
                Vector3 d = lp - center;

                bool inside =
                    includeBoundary
                        ? (Mathf.Abs(d.x) <= half.x && Mathf.Abs(d.y) <= half.y && Mathf.Abs(d.z) <= half.z)
                        : (Mathf.Abs(d.x) < half.x && Mathf.Abs(d.y) < half.y && Mathf.Abs(d.z) < half.z);

                // NEW: invert the selection if requested
                bool keep = invert ? !inside : inside;
                if (!keep) continue;

                worldVertices[write] = worldVertices[i];
                if (hasNormals) worldNormals[write] = worldNormals[i];
                write++;
            }

            if (write < worldVertices.Count)
            {
                worldVertices.RemoveRange(write, worldVertices.Count - write);
                if (hasNormals) worldNormals.RemoveRange(write, worldNormals.Count - write);
            }
        }

        private static void AppendWorldVerticesAndNormals(
           Mesh mesh,
           Transform meshTransform,
           List<Vector3> outWorldVerts,
           List<Vector3> outWorldNormals
       )
        {
            var verts = mesh.vertices;
            var norms = mesh.normals;

            outWorldVerts.Capacity = Mathf.Max(outWorldVerts.Capacity, outWorldVerts.Count + verts.Length);
            outWorldNormals.Capacity = Mathf.Max(outWorldNormals.Capacity, outWorldNormals.Count + verts.Length);

            bool hasNormals = norms != null && norms.Length == verts.Length;

            for (int i = 0; i < verts.Length; i++)
            {
                outWorldVerts.Add(meshTransform.TransformPoint(verts[i]));

                // If normals missing, fall back to up to keep list aligned
                Vector3 n = hasNormals ? meshTransform.TransformDirection(norms[i]).normalized : Vector3.up;
                outWorldNormals.Add(n);
            }
        }
        private static void AppendWorldVertices(Mesh mesh, Transform meshTransform, List<Vector3> outWorld)
        {
            var verts = mesh.vertices;
            outWorld.Capacity = Mathf.Max(outWorld.Capacity, outWorld.Count + verts.Length);

            for (int i = 0; i < verts.Length; i++)
            {
                outWorld.Add(meshTransform.TransformPoint(verts[i]));
            }
        }

        // Optional: mesh vertices often repeat due to topology/UV splits.
        private static void DeduplicateInPlace(List<Vector3> verts, float eps)
        {
            // Hash by quantized position (fast + stable enough for editor tooling)
            float inv = 1f / Mathf.Max(eps, 1e-9f);
            var seen = new HashSet<Vector3Int>();
            int write = 0;

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                var key = new Vector3Int(
                    Mathf.RoundToInt(v.x * inv),
                    Mathf.RoundToInt(v.y * inv),
                    Mathf.RoundToInt(v.z * inv)
                );

                if (seen.Add(key))
                {
                    verts[write++] = v;
                }
            }

            if (write < verts.Count)
                verts.RemoveRange(write, verts.Count - write);
        }

        private static void DeduplicateVertsAndNormalsInPlace(List<Vector3> verts, List<Vector3> norms, float eps)
        {
            float inv = 1f / Mathf.Max(eps, 1e-9f);

            var seen = new HashSet<Vector3Int>();
            int write = 0;

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                var key = new Vector3Int(
                    Mathf.RoundToInt(v.x * inv),
                    Mathf.RoundToInt(v.y * inv),
                    Mathf.RoundToInt(v.z * inv)
                );

                if (seen.Add(key))
                {
                    verts[write] = verts[i];
                    norms[write] = norms[i];
                    write++;
                }
            }

            if (write < verts.Count)
            {
                verts.RemoveRange(write, verts.Count - write);
                norms.RemoveRange(write, norms.Count - write);
            }
        }

    }
}
