namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    public sealed class FP_MeasurementBinder : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private FP_MeasurementToolController _tool;
        [SerializeField] private FP_MeasurementUIRaycaster _raycaster;
        [SerializeField] private FP_ToolbarBinder _toolbarBinder;

        [Header("Options")]
        [Tooltip("If true, clicking after Completed will reset and treat the click as a new first point.")]
        [SerializeField] private bool _restartOnCompletedClick = true;

        private void Reset()
        {
            _tool = GetComponent<FP_MeasurementToolController>();
            _raycaster = FindFirstObjectByType<FP_MeasurementUIRaycaster>();
        }

        private void OnEnable()
        {
            if (_raycaster != null)
            {
                _raycaster.OnMeasurementSelect += HandleMeasurementSelect;
                //_raycaster.enabled = (_tool != null && _tool.IsActive);
            }
            if (_toolbarBinder != null) 
            {
                _toolbarBinder.OnMeasureToolActivated += ActivateMeasurement;
                _toolbarBinder.OnMeasureToolDeactivated += DeactivateMeasurement;
                _toolbarBinder.OnMeasureToolReset += ResetMeasurement;
            }
        }

        private void OnDisable()
        {
            if (_raycaster != null)
            {
                _raycaster.OnMeasurementSelect -= HandleMeasurementSelect;
            }
            if (_toolbarBinder != null)
            {
                _toolbarBinder.OnMeasureToolActivated -= ActivateMeasurement;
                _toolbarBinder.OnMeasureToolDeactivated -= DeactivateMeasurement;
                _toolbarBinder.OnMeasureToolReset -= ResetMeasurement;
            }
        }

        #region External Call to activate/deactivate system
        public void ActivateMeasurement()
        {
            if (_tool == null) return;

            _tool.Activate(); // locks camera input via cam lock inside controller :contentReference[oaicite:3]{index=3}
            //if (_raycaster != null) _raycaster.enabled = true;
        }

        public void DeactivateMeasurement()
        {
            if (_tool == null) return;

            _tool.Deactivate(); // unlocks camera input :contentReference[oaicite:4]{index=4}
            //if (_raycaster != null) _raycaster.enabled = false;
        }

        public void ResetMeasurement()
        {
            if (_tool == null) return;

            _tool.ResetMeasure(); // returns to WaitingForA :contentReference[oaicite:5]{index=5}
        }
        #endregion

        private void HandleMeasurementSelect(FP_MeasurementHitProvider provider, RaycastHit hit)
        {
            if (_tool == null || !_tool.IsActive)
                return;

            // If we allow restart by clicking after completion
            if (_tool.State == FPMeasureState.Completed)
            {
                if (!_restartOnCompletedClick) return;

                _tool.ResetMeasure();
                // fallthrough: treat this click as "first point"
            }

            if (_tool.State == FPMeasureState.WaitingForA)
            {
                // First point MUST be a world hit on a measurable provider
                _tool.OnFirstPoint(hit.point);
                return;
            }

            if (_tool.State == FPMeasureState.WaitingForB)
            {
                // Perspective: second point uses world hit
                if (_tool.Mode == FPMeasureMode.Perspective)
                {
                    _tool.OnSecondPoint(hit.point);
                    return;
                }

                // OrthoPlane: second point is ray/plane intersection using the overlay camera
                if (_tool.Mode == FPMeasureMode.OrthoPlane)
                {
                    if (_raycaster == null || _raycaster.RaycastCamera == null)
                        return;

                    Ray r = _raycaster.RaycastCamera.ScreenPointToRay(_raycaster.LastPointerPos);

                    if (_tool.TryGetOrthoPlaneIntersection(r, out Vector3 p))
                        _tool.OnSecondPoint(p);

                    return;
                }
            }
        }
    }
}
