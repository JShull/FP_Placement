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
        public FPRuntimeMeasurementOverlay Overlay => _overlay;

        [Header("Visuals")]
        [SerializeField] private LineRenderer _line;
        [SerializeField] private TMPro.TextMeshPro _label;
        [SerializeField] private bool _isActive;

        [Header("Data")]
        [SerializeField] private FP_MeasurementHitProvider hitProvider;
        public bool IsActive => _isActive;
        public FPMeasureState State { get; private set; } = FPMeasureState.None;
        public FPMeasureMode Mode { get; private set; } = FPMeasureMode.None;

        [Header("Increment Snap")]
        [SerializeField] private bool _useAngleIncrement = false;
        [SerializeField, Range(1f, 45f)] private float _angleIncrementDegrees = 15f;

        public void SetAngleIncrementEnabled(bool enabled) => _useAngleIncrement = enabled;
        public void SetAngleIncrementDegrees(float degrees) => _angleIncrementDegrees = Mathf.Max(0.1f, degrees);

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
        // Snap/incremental will update accordingly based on Ortho/perspective
        public void OnSecondPoint(Vector3 worldPoint, FP_MeasurementHitProvider itemDetails)
        {
            if (!IsActive || State != FPMeasureState.WaitingForB) return;

            
            if (itemDetails != hitProvider)
            {
                Debug.LogWarning($"Second point came from another object - assuming the same scale here!!");
            }
            Vector3 bCandidate = worldPoint;
            if (_useAngleIncrement)
            {
                bCandidate = GetAngleSnappedPoint(_a, bCandidate);
            }
            _b = bCandidate;
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
        private Vector3 GetAngleSnappedPoint(Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            if (d.sqrMagnitude < 1e-10f) return b;

            // Pick plane normal:
            // - Ortho mode: use the ortho plane normal so snapping stays in the same plane
            // - Perspective: use camera forward for a stable user experience
            Vector3 n;
            if (Mode == FPMeasureMode.OrthoPlane)
                n = _orthoPlane.normal;
            else if (_camera != null)
                n = _camera.transform.forward;
            else
                n = Vector3.forward;

            // Build a stable 2D basis on the plane
            Vector3 xAxis = Vector3.ProjectOnPlane((_camera != null ? _camera.transform.right : Vector3.right), n).normalized;
            if (xAxis.sqrMagnitude < 1e-6f)
                xAxis = Vector3.ProjectOnPlane(Vector3.right, n).normalized;

            Vector3 yAxis = Vector3.Cross(n, xAxis).normalized;

            float x = Vector3.Dot(d, xAxis);
            float y = Vector3.Dot(d, yAxis);

            float len = Mathf.Sqrt(x * x + y * y);
            if (len < 1e-6f) return b;

            float stepRad = Mathf.Deg2Rad * Mathf.Max(0.1f, _angleIncrementDegrees);
            float ang = Mathf.Atan2(y, x);
            float snapped = Mathf.Round(ang / stepRad) * stepRad;

            float sx = Mathf.Cos(snapped) * len;
            float sy = Mathf.Sin(snapped) * len;

            Vector3 snappedD = xAxis * sx + yAxis * sy;

            // Preserve any component along the normal (for perspective hits that might not lie on plane)
            // For OrthoPlane, this should already be ~0.
            float nComp = Vector3.Dot(d, n);
            snappedD += n * nComp;

            return a + snappedD;
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
