namespace FuzzPhyte.Placement
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Packs transforms onto a finite quad area using 2D circle approximations of each object's bounds.
    /// Designed for editor-time random layout workflows.
    /// </summary>
    public static class QuadAreaPlacer
    {
        private struct PackedItem
        {
            public Transform Transform;
            public float Radius;
            public float Area;
        }

        private struct PlacedCircle
        {
            public Vector2 Position;
            public float Radius;
        }

        public static int ApplyToQuadArea(
            IReadOnlyList<Transform> items,
            Transform quadTransform,
            bool orientToSurface = true,
            int randomSeed = 0,
            float areaUsageLimit = 0.85f,
            float spacingPadding = 0.01f,
            int maxPlacementAttemptsPerItem = 64)
        {
            if (items == null || items.Count == 0 || quadTransform == null) return 0;

            Vector2 quadSize = GetQuadSizeFromTransform(quadTransform);
            float quadArea = Mathf.Abs(quadSize.x * quadSize.y);
            if (quadArea <= 0.000001f) return 0;

            float clampedUsage = Mathf.Clamp01(areaUsageLimit);
            if (clampedUsage <= 0f) return 0;

            List<PackedItem> candidates = BuildCandidates(items, spacingPadding);
            if (candidates.Count == 0) return 0;

            List<PackedItem> selected = SelectByAreaBudget(candidates, quadArea * clampedUsage);
            if (selected.Count == 0) return 0;

            var rng = (randomSeed == 0) ? new System.Random() : new System.Random(randomSeed);
            SortDescendingByRadius(selected);

            List<PlacedCircle> placed = new List<PlacedCircle>(selected.Count);
            float halfW = quadSize.x * 0.5f;
            float halfH = quadSize.y * 0.5f;

            int placedCount = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                PackedItem item = selected[i];
                Vector2 local2D;

                if (!TryFindPositionForCircle(item.Radius, placed, halfW, halfH, rng, maxPlacementAttemptsPerItem, out local2D))
                {
                    continue;
                }

                placed.Add(new PlacedCircle
                {
                    Position = local2D,
                    Radius = item.Radius
                });

                Vector3 local3 = new Vector3(local2D.x, 0f, local2D.y);
                Vector3 worldPos = quadTransform.TransformPoint(local3);
                item.Transform.position = worldPos;

                if (orientToSurface)
                {
                    Vector3 forward = Vector3.ProjectOnPlane(item.Transform.forward, quadTransform.up).normalized;
                    if (forward.sqrMagnitude < 0.000001f)
                    {
                        forward = quadTransform.right;
                    }

                    item.Transform.rotation = Quaternion.LookRotation(forward, quadTransform.up);
                }

                placedCount++;
            }

            return placedCount;
        }

        private static List<PackedItem> BuildCandidates(IReadOnlyList<Transform> items, float spacingPadding)
        {
            float pad = Mathf.Max(0f, spacingPadding);
            var list = new List<PackedItem>(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                Transform t = items[i];
                if (t == null) continue;

                float radius = ComputeBoundingSphereRadius(t) + pad;
                radius = Mathf.Max(0.001f, radius);

                list.Add(new PackedItem
                {
                    Transform = t,
                    Radius = radius,
                    Area = Mathf.PI * radius * radius
                });
            }

            return list;
        }

        private static List<PackedItem> SelectByAreaBudget(List<PackedItem> candidates, float allowedArea)
        {
            float runningArea = 0f;
            var selected = new List<PackedItem>(candidates.Count);

            for (int i = 0; i < candidates.Count; i++)
            {
                PackedItem c = candidates[i];
                if ((runningArea + c.Area) > allowedArea)
                {
                    break;
                }

                selected.Add(c);
                runningArea += c.Area;
            }

            return selected;
        }

        private static bool TryFindPositionForCircle(
            float radius,
            List<PlacedCircle> placed,
            float halfW,
            float halfH,
            System.Random rng,
            int maxAttempts,
            out Vector2 position)
        {
            position = Vector2.zero;

            if (radius > halfW || radius > halfH)
            {
                return false;
            }

            if (placed.Count == 0)
            {
                position = Vector2.zero;
                return true;
            }

            float bestScore = float.MaxValue;
            bool found = false;
            Vector2 best = Vector2.zero;

            int attempts = Mathf.Max(8, maxAttempts);
            for (int i = 0; i < attempts; i++)
            {
                float x = Mathf.Lerp(-halfW + radius, halfW - radius, (float)rng.NextDouble());
                float y = Mathf.Lerp(-halfH + radius, halfH - radius, (float)rng.NextDouble());
                Vector2 candidate = new Vector2(x, y);

                if (OverlapsAny(candidate, radius, placed))
                {
                    continue;
                }

                float score = ComputeScore(candidate, halfW, halfH);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                    found = true;
                }
            }

            if (found)
            {
                position = best;
                return true;
            }

            return false;
        }

        private static bool OverlapsAny(Vector2 candidate, float radius, List<PlacedCircle> placed)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                PlacedCircle p = placed[i];
                float minDist = radius + p.Radius;
                if ((candidate - p.Position).sqrMagnitude < (minDist * minDist))
                {
                    return true;
                }
            }

            return false;
        }

        // Lower is better: prefer center-out packing to keep usable border area coherent.
        private static float ComputeScore(Vector2 p, float halfW, float halfH)
        {
            float centerBias = p.sqrMagnitude;

            // Mildly penalize proximity to borders to avoid premature edge fragmentation.
            float marginX = halfW - Mathf.Abs(p.x);
            float marginY = halfH - Mathf.Abs(p.y);
            float borderPenalty = 1f / Mathf.Max(0.001f, marginX * marginY);

            return centerBias + borderPenalty;
        }

        private static void SortDescendingByRadius(List<PackedItem> items)
        {
            items.Sort((a, b) => b.Radius.CompareTo(a.Radius));
        }

        private static float ComputeBoundingSphereRadius(Transform t)
        {
            var renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return 0.25f;
            }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }

            return b.extents.magnitude;
        }

        public static Vector2 GetQuadSizeFromTransform(Transform quadTransform)
        {
            if (quadTransform == null)
            {
                return Vector2.zero;
            }

            Vector2 meshSize = TryGetQuadMeshSize(quadTransform);
            float sx = Mathf.Abs(quadTransform.lossyScale.x);
            float sz = Mathf.Abs(quadTransform.lossyScale.z);

            return new Vector2(meshSize.x * sx, meshSize.y * sz);
        }

        private static Vector2 TryGetQuadMeshSize(Transform quadTransform)
        {
            var filter = quadTransform.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                return Vector2.one;
            }

            Bounds localBounds = filter.sharedMesh.bounds;
            float width = Mathf.Abs(localBounds.size.x);
            float depth = Mathf.Abs(localBounds.size.z);

            if (width <= 0.000001f || depth <= 0.000001f)
            {
                return Vector2.one;
            }

            return new Vector2(width, depth);
        }
    }
}
