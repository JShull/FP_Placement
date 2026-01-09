namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;

    public sealed class FP_ModelOrbitalBoundsBinder : MonoBehaviour
    {
        [SerializeField] private FP_ModelCycleController _cycle;
        [SerializeField] private FP_OrbitalCameraBehaviour _orbital;

        private void OnEnable()
        {
            if (_cycle != null)
                _cycle.OnActiveModelChanged += HandleModelChanged;
        }

        private void OnDisable()
        {
            if (_cycle != null)
                _cycle.OnActiveModelChanged -= HandleModelChanged;
        }

        private void HandleModelChanged(int index, FP_ModelDisplayBinding binding)
        {
            if (_orbital == null || binding == null) return;

            Bounds wb = binding.GetWorldBounds();

            // You’ll need this method on your controller/behaviour if you don't already have it:
            // - SetBounds(bounds)
            // - optionally FitToBounds based on binding.Data.FitOnActivate
            _orbital.SetBounds(wb);

            if (binding.Data != null && binding.Data.OverrideProjectionOnActivate)
            {
                // this isn't correct - we need to resize the bounds
               // _orbital.Controller.SetProjection(binding.Data.ProjectionOnActivate);
            }

            if (binding.Data == null || binding.Data.FitOnActivate)
            {
                // Fit for current projection target
                _orbital.Controller.FitToBoundsForCurrentProjection();
            }
        }
    }
}
