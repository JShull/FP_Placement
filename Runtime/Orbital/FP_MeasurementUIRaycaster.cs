namespace FuzzPhyte.Placement.OrbitalCamera
{
    using System;
    using UnityEngine;

    public sealed class FP_MeasurementUIRaycaster:FP_UIRegionRaycasterBase<FP_MeasurementHitProvider>
    {
        public event Action<FP_MeasurementHitProvider, RaycastHit> OnMeasurementSelect;

        protected override string GetDebugTag() => "MeasurementRaycaster";

        protected override void OnSelect(FP_MeasurementHitProvider provider, RaycastHit hit)
        {
            if (provider == null || !provider.AllowMeasurement)
                return;

            OnMeasurementSelect?.Invoke(provider, hit);
        }
    }
}
