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
        public enum QuadSizeMode
        {
            MeshAndScale,      // Default: mesh bounds × lossy scale
            TransformScaleOnly // Ignore mesh, treat scale as the size
        }
        public enum QuadStartAnchor
        {
            Center,

            TopEdge,
            BottomEdge,
            LeftEdge,
            RightEdge,

            TopLeftCorner,
            TopRightCorner,
            BottomLeftCorner,
            BottomRightCorner
        }
        public enum PlacementSortMode
        {
            LargestFirst,
            SmallestFirst,
            None
        }
        public enum PlacementCapacityMode
        {
            NA,
            AreaBudget,
            FixedCount
        }

        public static int ApplyToQuadArea(
            IReadOnlyList<Transform> items,
            Transform quadTransform,
            bool orientToSurface = true,
            int randomSeed = 0,
            float areaUsageLimit = 0.85f,
            float spacingPadding = 0.01f,
            float borderPenaltyScale = 0.1f,
            int maxPlacementAttemptsPerItem = 64,
            float biasScaleInward=5,
            int aroundCircle=12,
            QuadStartAnchor startAnchor = QuadStartAnchor.Center,
            PlacementSortMode sortMode = PlacementSortMode.LargestFirst,
            QuadSizeMode sizeMode = QuadSizeMode.TransformScaleOnly)
        {
            if (items == null || items.Count == 0 || quadTransform == null) return 0;

            Vector2 quadSize = GetQuadSizeFromTransform(quadTransform,sizeMode);
            Debug.Log($"Quad Size: {quadSize}");
            float quadArea = Mathf.Abs(quadSize.x * quadSize.y);
            if (quadArea <= 0.000001f) return 0;

            float clampedUsage = Mathf.Clamp01(areaUsageLimit);
            if (clampedUsage <= 0f) return 0;
            //recenter items
            ResetItemsToQuadCenter(items, quadTransform, false);
            List<PackedItem> candidates = BuildCandidates(items, spacingPadding,quadTransform);
            if (candidates.Count == 0) return 0;

            // make sure we don't have an abnormally large item
            List<PackedItem> selected = SelectByAreaBudget(candidates, quadArea * clampedUsage);
            if (selected.Count == 0) return 0;

            var rng = (randomSeed == 0) ? new System.Random() : new System.Random(randomSeed);

            SortByRadius(selected,sortMode);

            List<PlacedCircle> placed = new List<PlacedCircle>(selected.Count);
            float halfW = quadSize.x * 0.5f;
            float halfH = quadSize.y * 0.5f;

            int placedCount = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                PackedItem item = selected[i];
                Vector2 local2D;

                if (!TryFindPositionForCircle(item.Radius, placed, halfW, halfH, rng, maxPlacementAttemptsPerItem, borderPenaltyScale,biasScaleInward, startAnchor, aroundCircle,out local2D))
                {
                    continue;
                }
                // local2D will be the position in quad local space
                // this works directly if our children items are under the quad scale
                // this doesn't work directly if our children items are not under that quad scale
                placed.Add(new PlacedCircle
                {
                    Position = local2D,
                    Radius = item.Radius
                });
                
                Vector3 local3 = new Vector3(local2D.x, 0f, local2D.y);
                //transform to quadTransform scale
                
                //Vector3 worldPos = quadTransform.TransformPoint(local3);
                Debug.Log($"Local Pos: {local3}");
                item.Transform.position = local3 + quadTransform.position;

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

        private static void ResetItemsToQuadCenter(IReadOnlyList<Transform> items,Transform quadTransform,bool orientToSurface)
        {
            Vector3 centerWorld = quadTransform.position;

            for (int i = 0; i < items.Count; i++)
            {
                Transform t = items[i];
                if (t == null) continue;

                // Move directly to quad center
                t.position = centerWorld;

                // Optional: align rotation immediately
                if (orientToSurface)
                {
                    t.rotation = Quaternion.LookRotation(
                        quadTransform.forward,
                        quadTransform.up
                    );
                }
            }
        }

        private static List<PackedItem> BuildCandidates(IReadOnlyList<Transform> items, float spacingPadding, Transform quadSpace)
        {
            float pad = Mathf.Max(0f, spacingPadding);
            var list = new List<PackedItem>(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                Transform t = items[i];
                if (t == null) continue;

              
                float radius = ComputeFootprintRadiusInQuadSpace(t, quadSpace);
                Debug.Log($"Radius for {t.gameObject.name}: {radius}");
                radius += pad * 0.5f;
                //padding is going in twice I think, cut in half?
                //float radius = ComputeBoundingSphereRadius(t) + pad*0.5f;
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
            float penaltyScalar,
            float inwardBiasScale,
            QuadStartAnchor startAnchor,
            int rotationsAround,
            out Vector2 position)
        {
            position = Vector2.zero;

            if (radius > halfW || radius > halfH)
            {
                return false;
            }

            if (placed.Count == 0)
            {
                position = GetAnchorStartPosition(startAnchor,radius,halfW,halfH);
                return true;
            }

            float bestScore = float.MaxValue;
            bool found = false;
            Vector2 best = Vector2.zero;

            /// First attempt: Tangent Candidate Pass
            /// 
            List<Vector2> tangentCandidates = new List<Vector2>();
            AddTangentCandidates(radius, placed, halfW, halfH, tangentCandidates, rotationsAround);

            for (int i = 0; i < tangentCandidates.Count; i++)
            {
                Vector2 candidate = tangentCandidates[i];

                float score = DistanceToNearestPlaced(candidate, placed);
                //float score = -CountTouches(candidate, radius, placed);
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
            /// Fallback Random Sampling
            /// 

            int attempts = Mathf.Max(8, maxAttempts);
            for (int i = 0; i < attempts; i++)
            {
                float x = Mathf.Lerp(-halfW + radius, halfW - radius, (float)rng.NextDouble());
                float y = Mathf.Lerp(-halfH + radius, halfH - radius, (float)rng.NextDouble());
                Vector2 candidate = new Vector2(x, y);

                if (!InsideQuad(candidate, radius, halfW, halfH))
                    continue;

                if (OverlapsAny(candidate, radius, placed))
                {
                    continue;
                }
                if (inwardBiasScale <= 0)
                {
                    inwardBiasScale = 5;
                }
                float score = ComputeScore(candidate, halfW, halfH, penaltyScalar, inwardBiasScale, startAnchor);
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
        private static Vector2 GetAnchorStartPosition(QuadStartAnchor anchor,float radius,float halfW,float halfH)
        {
            switch (anchor)
            {
                case QuadStartAnchor.TopEdge:
                    return new Vector2(0f, halfH - radius);

                case QuadStartAnchor.BottomEdge:
                    return new Vector2(0f, -halfH + radius);

                case QuadStartAnchor.LeftEdge:
                    return new Vector2(-halfW + radius, 0f);

                case QuadStartAnchor.RightEdge:
                    return new Vector2(halfW - radius, 0f);

                // ✅ CORNERS
                case QuadStartAnchor.TopLeftCorner:
                    return new Vector2(-halfW + radius, halfH - radius);

                case QuadStartAnchor.TopRightCorner:
                    return new Vector2(halfW - radius, halfH - radius);

                case QuadStartAnchor.BottomLeftCorner:
                    return new Vector2(-halfW + radius, -halfH + radius);

                case QuadStartAnchor.BottomRightCorner:
                    return new Vector2(halfW - radius, -halfH + radius);

                case QuadStartAnchor.Center:
                default:
                    return Vector2.zero;
            }
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
        private static float ComputeScore(Vector2 p, float halfW, float halfH, float penaltyScale, float inwardBiasScale,QuadStartAnchor anchor)
        {
            float inwardBias = 0f;

            Vector2 inwardDir = GetInwardDirection(anchor);
            if (inwardDir != Vector2.zero)
            {
                // Project position onto inward direction
                inwardBias = Vector2.Dot(p, inwardDir) * inwardBiasScale;
            }
            else
            {
                // Center mode
                inwardBias = p.sqrMagnitude;
            }

            // Mildly penalize proximity to borders to avoid premature edge fragmentation.
            float marginX = halfW - Mathf.Abs(p.x);
            float marginY = halfH - Mathf.Abs(p.y);
            float borderPenalty = 1f / Mathf.Max(0.001f, marginX * marginY);

            return inwardBias + borderPenalty * penaltyScale;

        }
        private static void SortByRadius( List<PackedItem> items,PlacementSortMode mode)
        {
            switch (mode)
            {
                case PlacementSortMode.LargestFirst:
                    items.Sort((a, b) => b.Radius.CompareTo(a.Radius));
                    break;

                case PlacementSortMode.SmallestFirst:
                    items.Sort((a, b) => a.Radius.CompareTo(b.Radius));
                    break;

                case PlacementSortMode.None:
                default:
                    // Keep original order
                    break;
            }
        }

        
        private static float ComputeFootprintRadiusInQuadSpace(Transform item,Transform quadTransform)
        {
            var renderers = item.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"No Renderers Found. {0.25f} set as a radius fallback");
                return 0.25f;
            }

            float maxDist = 0f;

            for (int r=0;r< renderers.Length;r++)
            {
                var AR = renderers[r];
                Bounds lb = AR.bounds;
                Vector3 c = lb.center;
                Vector3 e = lb.extents;

                // 8 local corners in renderer space
                Vector3[] corners =
                {
                    c + new Vector3( e.x,  e.y,  e.z),
                    c + new Vector3( e.x,  e.y, -e.z),
                    c + new Vector3( e.x, -e.y,  e.z),
                    c + new Vector3( e.x, -e.y, -e.z),
                    c + new Vector3(-e.x,  e.y,  e.z),
                    c + new Vector3(-e.x,  e.y, -e.z),
                    c + new Vector3(-e.x, -e.y,  e.z),
                    c + new Vector3(-e.x, -e.y, -e.z),
                };

                for (int i = 0; i < corners.Length; i++)
                {
                    // Convert corner to world space properly
                    Vector3 worldCorner = AR.transform.TransformPoint(corners[i]);

                    // Convert into quad local space
                    Vector3 quadLocal = quadTransform.InverseTransformPoint(worldCorner);

                    // Footprint distance in quad plane
                    Vector2 flat = new Vector2(quadLocal.x, quadLocal.z);

                    maxDist = Mathf.Max(maxDist, flat.magnitude);
                }
            }
            //Debug.Log($"Item: {item.gameObject.name} w/ Footprint Radius: {maxDist}");
            return maxDist;
        }

        private static Vector2 GetInwardDirection(QuadStartAnchor anchor)
        {
            switch (anchor)
            {
                case QuadStartAnchor.TopEdge: return Vector2.down;
                case QuadStartAnchor.BottomEdge: return Vector2.up;
                case QuadStartAnchor.LeftEdge: return Vector2.right;
                case QuadStartAnchor.RightEdge: return Vector2.left;

                //CORNERS (Diagonal inward)
                case QuadStartAnchor.TopLeftCorner: return (Vector2.down + Vector2.right).normalized;
                case QuadStartAnchor.TopRightCorner: return (Vector2.down + Vector2.left).normalized;
                case QuadStartAnchor.BottomLeftCorner: return (Vector2.up + Vector2.right).normalized;
                case QuadStartAnchor.BottomRightCorner: return (Vector2.up + Vector2.left).normalized;

                default:
                    return Vector2.zero;
            }
        }


        public static Vector2 GetQuadSizeFromTransform(Transform quadTransform, QuadSizeMode mode=QuadSizeMode.TransformScaleOnly)
        {
            if (quadTransform == null)
            {
                return Vector2.zero;
            }
            float sx = Mathf.Abs(quadTransform.lossyScale.x);
            float sz = Mathf.Abs(quadTransform.lossyScale.z);

            switch (mode)
            {
                case QuadSizeMode.TransformScaleOnly:
                    // Treat scale as literal size
                    return new Vector2(sx, sz);

                case QuadSizeMode.MeshAndScale:
                default:
                    Vector2 meshSize = TryGetQuadMeshSize(quadTransform);
                    return new Vector2(meshSize.x * sx, meshSize.y * sz);
            }
        }

        private static float DistanceToNearestPlaced(Vector2 candidate, List<PlacedCircle> placed)
        {
            float best = float.MaxValue;

            for (int i = 0; i < placed.Count; i++)
            {
                float d = Vector2.Distance(candidate, placed[i].Position);
                if (d < best)
                    best = d;
            }

            return best;
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
        private static void AddTangentCandidates(float radius, List<PlacedCircle> placed, float halfW, float halfH, List<Vector2> candidates, int sizeK=12)
        {
            //Debug.Log($"Candidates********");
            for (int i = 0; i < placed.Count; i++)
            {
                PlacedCircle other = placed[i];

                float touchDist = radius + other.Radius;

                // Try several angles around this circle
                for (int k = 0; k < sizeK; k++)
                {
                    float angle = (Mathf.PI * 2f) * ((float)k / sizeK);
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    Vector2 candidate = other.Position + dir * touchDist;
                    //Debug.Log($"Candidate pos: {candidate}, angle: {angle}, touch Dist: {touchDist}, direction: {dir}");
                    if (!InsideQuad(candidate, radius, halfW, halfH))
                        continue;

                    if (!OverlapsAny(candidate, radius, placed))
                        candidates.Add(candidate);
                }
            }
        }
        private static bool InsideQuad(Vector2 p, float r, float halfW, float halfH)
        {
            return p.x - r >= -halfW &&
                   p.x + r <= halfW &&
                   p.y - r >= -halfH &&
                   p.y + r <= halfH;
        }
    }
}
