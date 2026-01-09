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
        public Vector3 LocalBoundsCenter;

        [Tooltip("Size in local space.")]
        public Vector3 LocalBoundsSize = Vector3.one;

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

        [Tooltip("Preferred projection when activated (optional).")]
        public bool OverrideProjectionOnActivate;

        public FP_ProjectionMode ProjectionOnActivate = FP_ProjectionMode.Perspective;

        [Header("Feature Flags")]
        public bool SupportsVertices;
        public bool SupportsWireframe;
    }
}
