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

            Bounds wb = new Bounds();
            if (binding.Data.UseLocalBoundsOverride)
            {
                wb = binding.GetLocalBounds();
            }
            else
            {
                wb = binding.GetWorldBounds();
            }
                
            // call SetBounds syncs controller and data
            _orbital.SetBounds(wb);
            //set my debug "box collider"
            _orbital.TargetBounds.size = wb.size;
            //JOHN --> still need to now resize my camera based on this information (max/min zoom relative)
            _orbital.ResetCameraMaxDistance();
            //JOHN --> Check if we set the Visual Information here or..

        }
    }
}
