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
            {
                _cycle.OnActiveModelChanged += HandleModelChanged;
            }
                
        }

        private void OnDisable()
        {
            if (_cycle != null)
            {
                _cycle.OnActiveModelChanged -= HandleModelChanged;
            }
                
        }

        private void HandleModelChanged(int index, FP_ModelDisplayBinding binding)
        {
            if (_orbital == null || binding == null) return;

            _orbital.FocusBounds(binding.GetWorldBounds(), binding.transform, true);
        }
    }
}
