namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using FuzzPhyte.Utility;
    public sealed class FP_MeasurementHitProvider : MonoBehaviour
    {
        [Header("Optional Metadata")]
        public string DisplayName;
        [Tooltip("If false, this object will be ignored by the measurement raycaster.")]
        public bool AllowMeasurement = true;
        [Tooltip("Make sure your model is in accurate scale in meters")]
        public UnitOfMeasure ModelUnits = UnitOfMeasure.Inch;
        //[Tooltip("Adjust this as needed - but if already in true scale should be 1")]
        //public float ScaleAdjustment = 1.0f;
    }
}
