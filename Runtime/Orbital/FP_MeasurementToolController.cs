namespace FuzzPhyte.Placement.OrbitalCamera
{
    using FuzzPhyte.Utility;
    using UnityEngine;

    public sealed class FP_MeasurementToolController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private FP_OrbitalCameraToolLock _camLock;
        [SerializeField] private Camera _camera;
        [SerializeField] private FPRuntimeMeasurementOverlay _overlay;

        [Header("Visuals")]
        [SerializeField] private LineRenderer _line;
        [SerializeField] private TMPro.TextMeshPro _label;
        [SerializeField] private bool _isActive;

        [Header("Data")]
        [SerializeField] private FP_MeasurementHitProvider hitProvider;
        public bool IsActive => _isActive;
        public FPMeasureState State { get; private set; } = FPMeasureState.None;
        public FPMeasureMode Mode { get; private set; } = FPMeasureMode.None;

        private Vector3 _a;
        private Vector3 _b;
        private Plane _orthoPlane;

        private void Awake()
        {
            if (_camera == null && _camLock != null) _camera = _camLock.Camera;
            if (_overlay == null) _overlay = GetComponent<FPRuntimeMeasurementOverlay>();
            // start off
            _isActive = false;
        }
        #region State Change Based Functions
        public void Activate()
        {
            _isActive = true;
            State = FPMeasureState.WaitingForA;
            Mode = FPMeasureMode.None;

            if (_camLock != null) _camLock.SetToolInputLocked(true);
            ClearVisuals();
        }

        public void Deactivate()
        {
            _isActive = false;
            State = FPMeasureState.None;
            Mode = FPMeasureMode.None;

            if (_camLock != null) _camLock.SetToolInputLocked(false);
            //ClearVisuals();
        }

        public void ResetMeasure()
        {
            if (!IsActive) return;
            State = FPMeasureState.WaitingForA;
            Mode = FPMeasureMode.None;
            ClearVisuals();
        }
        /// <summary>
        /// Called externally when we have another system step in
        /// </summary>
        public void ResetWithDeactivate()
        {
            _isActive = false;
            State = FPMeasureState.None;
            Mode = FPMeasureMode.None;
            ClearVisuals();
        }
        #endregion
        // Call this from your raycast/binder when user clicks a valid hit
        public void OnFirstPoint(Vector3 worldPoint, FP_MeasurementHitProvider itemDetails)
        {
            if (!IsActive || State != FPMeasureState.WaitingForA) return;

            _a = worldPoint;
            hitProvider = itemDetails;
            bool ortho = _camera != null && _camera.orthographic;
            Mode = ortho ? FPMeasureMode.OrthoPlane : FPMeasureMode.Perspective;

            if (Mode == FPMeasureMode.OrthoPlane)
            {
                // Plane through A, facing camera forward
                _orthoPlane = new Plane(_camera.transform.forward, _a);
            }

            State = FPMeasureState.WaitingForB;
            if (itemDetails != null)
            {
                UpdateVisual(_a, _a,itemDetails.ModelUnits);
            }
            else
            {
                UpdateVisual(_a, _a, UnitOfMeasure.Meter);
            }
        }

        // Perspective: binder should pass a world-hit point.
        // OrthoPlane: binder can pass the screen ray and we intersect it here.
        public void OnSecondPoint(Vector3 worldPoint, FP_MeasurementHitProvider itemDetails)
        {
            if (!IsActive || State != FPMeasureState.WaitingForB) return;

            
            if (itemDetails != hitProvider)
            {
                Debug.LogWarning($"Second point came from another object - assuming the same scale here!!");
            }
            _b = worldPoint;
            State = FPMeasureState.Completed;
            if (itemDetails != null)
            {
                UpdateVisual(_a, _b, itemDetails.ModelUnits);
            }
            else
            {
                UpdateVisual(_a, _b,UnitOfMeasure.Meter);
            }
        }

        public bool TryGetOrthoPlaneIntersection(Ray ray, out Vector3 hit)
        {
            hit = default;
            if (Mode != FPMeasureMode.OrthoPlane) return false;

            if (_orthoPlane.Raycast(ray, out float enter))
            {
                hit = ray.GetPoint(enter);
                return true;
            }
            return false;
        }

        private void UpdateVisual(Vector3 a, Vector3 b, UnitOfMeasure units)
        {
            if (_overlay != null)
            {
                _overlay.SetMeasurement(a, b, true, units);
            }

            if (_line != null)
            {
                _line.positionCount = 2;
                _line.SetPosition(0, a);
                _line.SetPosition(1, b);
                _line.enabled = true;
            }

            float d = Vector3.Distance(a, b);

            if (_label != null)
            {
                (bool gotValue, float distance)=FP_UtilityData.ConvertValue(d, UnitOfMeasure.Meter, units);
                if (gotValue)
                {
                    var unitAbb = FP_UtilityData.GetUnitAbbreviation(units);
                    _label.text = $"{distance:0.##} {unitAbb}";
                }
                else
                {
                    _label.text = $"{d:0.###} m";
                }
                _label.transform.position = (a + b) * 0.5f;
                _label.gameObject.SetActive(true);
            }
        }

        private void ClearVisuals()
        {
            if (_line != null) _line.enabled = false;
            if (_label != null) _label.gameObject.SetActive(false);
            if (_overlay != null) _overlay.SetMeasurement(Vector3.zero, Vector3.zero, false, UnitOfMeasure.Meter);
        }
    }
}
