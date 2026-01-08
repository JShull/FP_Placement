namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using System;

    public sealed class FP_ToolbarUIRaycaster : FP_UIRegionRaycasterBase<FP_ToolbarHitProvider>
    {
        public event Action<FP_ToolbarAction, FP_ToolbarHitProvider, RaycastHit> OnToolbarAction;

        protected override string GetDebugTag() => "FP_ToolbarUIRaycaster";

        protected override void OnSelect(FP_ToolbarHitProvider provider, RaycastHit hit)
        {
            // optional visuals
            provider.Select();

            OnToolbarAction?.Invoke(provider.Action, provider, hit);
        }

        protected override void OnHover(FP_ToolbarHitProvider provider, RaycastHit hit)
        {
            provider.Hover();
        }

        protected override void OnUnHover(FP_ToolbarHitProvider provider)
        {
            provider.UnHover();
        }

        protected override void OnUnSelect(FP_ToolbarHitProvider provider)
        {
            provider.UnSelect();
        }
    }
}
