namespace FuzzPhyte.Placement
{
    using FuzzPhyte.Utility;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public enum StackSuitability
    {
        Heavy,
        Medium,
        Light,
        Fragile
    }
    
    public enum ShapeType
    {
        Circle,
        Ellipse,
        Rectangle
    }
    public enum PlacementBuildMode
    {
        Stacking,
        Layout
    }
    public enum LayoutSurfaceType
    {
        SphereSurface,
        BoxSurface,
        MeshSurface
    }
    public enum LayoutDistribution
    {
        Even,
        Random
    }
    public enum SphereEvenMode
    {
        Fibonacci, 
        LatLonRings
    }

    [System.Serializable]
    [CreateAssetMenu(fileName = "PlacementObject", menuName = "FuzzPhyte/Placement/Object", order = 11)]
    public class PlacementObject : FP_Data
    {
        [Header("Placement Settings")]
        public Vector3 Normal = Vector3.up;
        public List<Vector2> FootprintPoints = new();
        public float FootprintSize = 0.5f;
        public Bounds BoundingBox = new(Vector3.zero, Vector3.one);

        [Header("Build Mode")]
        public PlacementBuildMode BuildMode = PlacementBuildMode.Stacking;

        [Header("Stacking Area Settings")]
        public bool Stackable = true;
        public StackSuitability StackSuitability = StackSuitability.Medium;
        public ShapeType Shape = ShapeType.Circle;
        public Vector2 StackSize = new Vector2(0.5f, 0.5f);
        public Vector3 StackCenterOffset = Vector3.up * 0.5f;

        [Header("Layout Settings (3D)")]
        public LayoutSurfaceType LayoutSurface = LayoutSurfaceType.SphereSurface;
        public LayoutDistribution LayoutDistribution = LayoutDistribution.Even;
        public SphereEvenMode SphereEvenMode = SphereEvenMode.Fibonacci;
        // NEW (only used for LatLonRings)
        [Min(2)]
        public int LatLonRingCount = 8; // number of latitude bands/rings
        public int LayoutSeed = 12345;
        public bool LayoutOrientOutward = true;

        [Header("Layout: Sphere Surface")]
        public float SphereRadius = 1.0f;
        [Tooltip("Degrees: 0..180")]
        public Vector2 ThetaRangeDeg = new Vector2(0f, 180f);
        [Tooltip("Degrees: 0..360")]
        public Vector2 PhiRangeDeg = new Vector2(0f, 360f);

        [Header("Layout: Box Surface")]
        public Vector3 BoxSize = Vector3.one;
        public Vector3 BoxCenterOffset = Vector3.zero;

        [Header("Layout: Mesh Surface")]
        public Mesh MeshSurfaceSource;
        public bool MeshRemoveDuplicateVertices = true;
        public float MeshDuplicateEpsilon = 0.0001f;
        public MeshVertexPickMode MeshPickMode = MeshVertexPickMode.EvenInOrder;
        public bool MeshIncludeSkinned = true;
        [Header("Category Links")]
        public List<PlacementCategory> Categories = new(); // CEFR level, tags, vocab category
        public List<string> VocabularyTags = new(); // Optional tags for vocabulary or thematic grouping
        

        public Vector3 GetRandomPointLocal()
        {
            Vector2 local2D = Vector2.zero;
            switch (Shape)
            {
                case ShapeType.Circle:
                    float angleC = Random.Range(0f, Mathf.PI * 2f);
                    float radiusC = Random.Range(0f, StackSize.x);
                    local2D = new Vector2(Mathf.Cos(angleC), Mathf.Sin(angleC)) * radiusC;
                    break;
                case ShapeType.Ellipse:
                    float angleE = Random.Range(0f, Mathf.PI * 2f);
                    float rE = Mathf.Sqrt(Random.Range(0f, 1f)); // Uniform distribution
                    local2D = new Vector2(rE * StackSize.x * Mathf.Cos(angleE), rE * StackSize.y * Mathf.Sin(angleE));
                    break;
                case ShapeType.Rectangle:
                    local2D = new Vector2(
                        Random.Range(-StackSize.x * 0.5f, StackSize.x * 0.5f),
                        Random.Range(-StackSize.y * 0.5f, StackSize.y * 0.5f)
                    );
                    break;
            }
            return StackCenterOffset + new Vector3(local2D.x, 0f, local2D.y);
        }

        // NEW: deterministic sphere point
        public Vector3 GetLayoutPointSphereLocal(int index, int count)
        {
            // Clamp ranges to valid values
            float theta0 = Mathf.Clamp(ThetaRangeDeg.x, 0f, 180f);
            float theta1 = Mathf.Clamp(ThetaRangeDeg.y, 0f, 180f);
            float phi0 = PhiRangeDeg.x;
            float phi1 = PhiRangeDeg.y;

            if (count <= 1) count = 1;

            // Even: Fibonacci-ish by index. Random: seeded random per index.
            float thetaDeg;
            float phiDeg;

            if (LayoutDistribution == LayoutDistribution.Random)
            {
                var rng = new System.Random(LayoutSeed ^ (index * 73856093));
                thetaDeg = Mathf.Lerp(theta0, theta1, (float)rng.NextDouble());
                phiDeg = Mathf.Lerp(phi0, phi1, (float)rng.NextDouble());
            }
            else
            {
                // lat long mode
                if (SphereEvenMode == SphereEvenMode.LatLonRings)
                {
                    // Latitude rings + longitude steps per ring
                    int rings = Mathf.Max(2, LatLonRingCount);

                    // Map index -> ring -> slot on ring
                    // We allocate "capacity" per ring proportional to sin(theta), so equatorial rings get more points.
                    float thetaSpan = Mathf.Abs(theta1 - theta0);
                    float phiSpan = Mathf.Abs(phi1 - phi0);
                    if (phiSpan < 0.0001f) phiSpan = 360f;

                    // Build ring weights and capacities
                    float[] ringThetaDeg = new float[rings];
                    float[] ringWeight = new float[rings];
                    int[] ringCap = new int[rings];

                    float weightSum = 0f;
                    for (int r = 0; r < rings; r++)
                    {
                        float t = (rings == 1) ? 0.5f : r / (float)(rings - 1);
                        float th = Mathf.Lerp(theta0, theta1, t);
                        ringThetaDeg[r] = th;

                        // Weight by sin(theta) -> more slots around equator
                        float w = Mathf.Sin(th * Mathf.Deg2Rad);
                        w = Mathf.Max(0.0001f, w);
                        ringWeight[r] = w;
                        weightSum += w;
                    }

                    int allocated = 0;
                    for (int r = 0; r < rings; r++)
                    {
                        int cap = Mathf.RoundToInt((ringWeight[r] / weightSum) * count);
                        cap = Mathf.Max(1, cap);
                        ringCap[r] = cap;
                        allocated += cap;
                    }

                    // Fix allocation to match count exactly
                    while (allocated > count)
                    {
                        // remove from the largest-cap ring (but keep >=1)
                        int best = -1;
                        int bestCap = 0;
                        for (int r = 0; r < rings; r++)
                        {
                            if (ringCap[r] > bestCap)
                            {
                                bestCap = ringCap[r];
                                best = r;
                            }
                        }

                        if (best >= 0 && ringCap[best] > 1)
                        {
                            ringCap[best]--;
                            allocated--;
                        }
                        else break;
                    }

                    while (allocated < count)
                    {
                        // add to the highest-weight ring
                        int best = 0;
                        float bestW = ringWeight[0];
                        for (int r = 1; r < rings; r++)
                        {
                            if (ringWeight[r] > bestW)
                            {
                                bestW = ringWeight[r];
                                best = r;
                            }
                        }

                        ringCap[best]++;
                        allocated++;
                    }

                    // Find which ring this index belongs to
                    int ringIndex = 0;
                    int slotIndex = index;

                    for (int r = 0; r < rings; r++)
                    {
                        if (slotIndex < ringCap[r])
                        {
                            ringIndex = r;
                            break;
                        }
                        slotIndex -= ringCap[r];
                    }

                    int slotsOnRing = ringCap[ringIndex];

                    thetaDeg = ringThetaDeg[ringIndex];

                    // Longitude within range, evenly spaced by slot
                    float slotT = (slotsOnRing == 1) ? 0.5f : (slotIndex / (float)slotsOnRing);
                    phiDeg = phi0 + slotT * phiSpan;

                    // Optional: stagger alternating rings so points don't line up in vertical columns
                    if ((ringIndex & 1) == 1 && slotsOnRing > 1)
                    {
                        phiDeg += (0.5f / slotsOnRing) * phiSpan;
                    }
                }
                else {
                    // Even-ish distribution:
                    // Use a golden angle progression for phi; use index stratification for theta.
                    float t = (count == 1) ? 0.5f : (index / (float)(count - 1));
                    thetaDeg = Mathf.Lerp(theta0, theta1, t);

                    const float goldenAngle = 137.50776405f;
                    phiDeg = phi0 + (index * goldenAngle);
                    // Wrap into [phi0, phi1] if you want a bounded range
                    float span = Mathf.Abs(phi1 - phi0);
                    if (span > 0.0001f)
                    {
                        float wrapped = Mathf.Repeat(phiDeg - phi0, span);
                        phiDeg = phi0 + wrapped;
                    }
                }
            }

            float theta = thetaDeg * Mathf.Deg2Rad;
            float phi = phiDeg * Mathf.Deg2Rad;

            float x = SphereRadius * Mathf.Sin(theta) * Mathf.Cos(phi);
            float y = SphereRadius * Mathf.Cos(theta);
            float z = SphereRadius * Mathf.Sin(theta) * Mathf.Sin(phi);

            return new Vector3(x, y, z);
        }

        // NEW: deterministic box surface point
        public Vector3 GetLayoutPointBoxLocal(int index, int count, out Vector3 outwardNormal)
        {
            outwardNormal = Vector3.up;

            if (count <= 1) count = 1;

            Vector3 half = BoxSize * 0.5f;
            // Choose face (6 faces) proportionally by area
            float ax = Mathf.Abs(BoxSize.x);
            float ay = Mathf.Abs(BoxSize.y);
            float az = Mathf.Abs(BoxSize.z);

            float areaXY = ax * ay;
            float areaXZ = ax * az;
            float areaYZ = ay * az;

            float totalArea = 2f * (areaXY + areaXZ + areaYZ);
            if (totalArea <= 0.00001f) totalArea = 1f;

            float f;
            if (LayoutDistribution == LayoutDistribution.Random)
            {
                var rng = new System.Random(LayoutSeed ^ (index * 19349663));
                f = (float)rng.NextDouble();
            }
            else
            {
                // Even: spread indices across total surface area
                f = (index + 0.5f) / count;
            }

            // Map f into a face selection by cumulative area
            float target = f * totalArea;

            // Each face area:
            // +X and -X faces are YZ (areaYZ)
            // +Y and -Y faces are XZ (areaXZ)
            // +Z and -Z faces are XY (areaXY)
            // Build cumulative:
            // 0..areaYZ => +X
            // areaYZ..2*areaYZ => -X
            // then +Y, -Y (areaXZ each)
            // then +Z, -Z (areaXY each)
            float c0 = areaYZ;
            float c1 = c0 + areaYZ;
            float c2 = c1 + areaXZ;
            float c3 = c2 + areaXZ;
            float c4 = c3 + areaXY;
            float c5 = c4 + areaXY;

            int face;
            if (target < c0) face = 0;        // +X
            else if (target < c1) face = 1;   // -X
            else if (target < c2) face = 2;   // +Y
            else if (target < c3) face = 3;   // -Y
            else if (target < c4) face = 4;   // +Z
            else face = 5;                    // -Z

            // For coordinates on the face, generate 2D params u,v
            float u, v;
            if (LayoutDistribution == LayoutDistribution.Random)
            {
                var rng = new System.Random((LayoutSeed + 17) ^ (index * 83492791) ^ (face * 2654435761u.GetHashCode()));
                u = (float)rng.NextDouble();
                v = (float)rng.NextDouble();
            }
            else
            {
                // Even-ish: use a simple grid inferred from count
                // (not perfect per-face, but stable)
                int n = Mathf.CeilToInt(Mathf.Sqrt(count));
                int ix = index % n;
                int iy = index / n;
                u = (n <= 1) ? 0.5f : ix / (float)(n - 1);
                v = (n <= 1) ? 0.5f : Mathf.Repeat(iy / (float)(n - 1), 1f);
            }

            // Convert u,v to [-half..half]
            float px = Mathf.Lerp(-half.x, half.x, u);
            float py = Mathf.Lerp(-half.y, half.y, u); // placeholder; will be overridden per face
            float pz = Mathf.Lerp(-half.z, half.z, v);

            Vector3 p = Vector3.zero;

            switch (face)
            {
                case 0: // +X face: x = +half.x, y,z vary
                    p = new Vector3(+half.x, Mathf.Lerp(-half.y, half.y, u), Mathf.Lerp(-half.z, half.z, v));
                    outwardNormal = Vector3.right;
                    break;
                case 1: // -X
                    p = new Vector3(-half.x, Mathf.Lerp(-half.y, half.y, u), Mathf.Lerp(-half.z, half.z, v));
                    outwardNormal = Vector3.left;
                    break;
                case 2: // +Y: y = +half.y, x,z vary
                    p = new Vector3(Mathf.Lerp(-half.x, half.x, u), +half.y, Mathf.Lerp(-half.z, half.z, v));
                    outwardNormal = Vector3.up;
                    break;
                case 3: // -Y
                    p = new Vector3(Mathf.Lerp(-half.x, half.x, u), -half.y, Mathf.Lerp(-half.z, half.z, v));
                    outwardNormal = Vector3.down;
                    break;
                case 4: // +Z: z = +half.z, x,y vary
                    p = new Vector3(Mathf.Lerp(-half.x, half.x, u), Mathf.Lerp(-half.y, half.y, v), +half.z);
                    outwardNormal = Vector3.forward;
                    break;
                case 5: // -Z
                    p = new Vector3(Mathf.Lerp(-half.x, half.x, u), Mathf.Lerp(-half.y, half.y, v), -half.z);
                    outwardNormal = Vector3.back;
                    break;
            }

            return BoxCenterOffset + p;
        }

    }
}
