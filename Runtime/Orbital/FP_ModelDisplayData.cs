namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "FP_ModelDisplayData", menuName = "FuzzPhyte/Placement/Model Meta Data")]
    public sealed class FP_ModelDisplayData : ScriptableObject
    {
        [Header("Identity")]
        public string DisplayName;

        [Header("Bounds")]
        [Tooltip("Optional explicit bounds override in LOCAL space of the model root.")]
        public bool UseLocalBoundsOverride;

        [Tooltip("Center in local space.")]
        public Vector3 BoundsCenter;

        [Tooltip("Size in local space.")]
        public Vector3 BoundsSize = Vector3.one;

        [Header("Optional Padding")]
        [Min(1f)] public float BoundsPadding = 1.0f;

        [Header("Placement")]
        [Tooltip("Optional local offset applied when placing at the display pivot.")]
        public Vector3 LocalPivotOffset;

        [Tooltip("Default local rotation when activated (e.g., to present best angle).")]
        public Vector3 DefaultLocalEuler;

        [Tooltip("Default uniform scale multiplier applied to the model root.")]
        public float DefaultUniformScale = 1f;

        [Header("Camera Behavior")]
        [Tooltip("If true, when this model becomes active, the orbital system should fit bounds.")]
        public bool FitOnActivate = true;

        [Header("Feature Flags")]
        public bool SupportsVertices;
        public bool SupportsWireframe;

        public Bounds GetLocalBounds()
        {
            var b = new Bounds(BoundsCenter, BoundsSize);
            b.extents *= BoundsPadding;
            return b;
        }
    }
}
