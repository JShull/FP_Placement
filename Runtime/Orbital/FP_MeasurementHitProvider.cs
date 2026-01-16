namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;

    public sealed class FP_MeasurementHitProvider : MonoBehaviour
    {
        [Header("Optional Metadata")]
        public string DisplayName;
        [Tooltip("If false, this object will be ignored by the measurement raycaster.")]
        public bool AllowMeasurement = true;
    }
}
