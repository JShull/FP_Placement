namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using System;
    using FuzzPhyte.Utility;

    /// <summary>
    /// Optional convenience MonoBehaviour wrapper for Unity scenes.
    /// Still UI-agnostic: you can call FeedInput() from anywhere.
    /// </summary>
    public sealed class FP_OrbitalCameraBehaviour : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private FP_OrbitalCameraSettings _settings = new FP_OrbitalCameraSettings();

        private FP_OrbitalCameraController _controller;
        private FP_OrbitalInput _queuedInput = FP_OrbitalInput.None;
        [SerializeField]private Vector3 localOffCenterCache = Vector3.zero;
        public FP_OrbitalCameraController Controller => _controller;
        public BoxCollider TargetBounds;
        [Header("Rotation Event Parameters")]
        [SerializeField] private float _rotationEventThresholdDegrees = 0.05f;
        [Space]
        [Header("Plane Constraint")]
        public bool RestrictBelowPlane = false;
        [Tooltip("Use the bounds information to move the plane to align")]
        public bool MovePlaneBasedOnBounds = true;
        [SerializeField]public Transform _planeReference;
        public Vector3 PlaneNormal = Vector3.up;
        public float PlaneOffset = 0f;
        [Header("Secondary Plane Constraint")]
        public bool RestrictSecondaryPlane = false;
        [SerializeField] public Transform _secondaryPlaneReference;
        public Vector3 SecondaryPlaneNormal = Vector3.up;
        public float SecondaryPlaneOffset = 0f;
        [SerializeField] FP_UtilityDraw _debugPlanes;
        /// <summary>
        /// Fired after the camera transform has been applied (LateUpdate).
        /// Subscribers can sync UI (e.g., ViewCube) to match the camera view.
        /// </summary>
        public event Action<FP_OrbitalViewState> OnCameraRotationApplied;

        private Quaternion _lastRotation;

        private void Awake()
        {
            if (!_camera) _camera = GetComponentInChildren<Camera>();
            _controller = new FP_OrbitalCameraController(_camera, _settings);
            
            if (TargetBounds != null)
            {
                float checkMaxDistance = 0;
                var newCenter = _controller.SetTargetBounds(TargetBounds.bounds, Vector3.zero,null);
                checkMaxDistance = TargetBounds.bounds.size.magnitude*1.1f;
                _controller.ZoomToFitBounds(checkMaxDistance);
                if (RestrictBelowPlane && MovePlaneBasedOnBounds)
                {
                    UpdateRestrictBelowPlaneFromBounds(newCenter);
                }
                else if(RestrictBelowPlane && _planeReference!=null)
                {

                    UpdateRestrictBelowPlaneFromBounds(_planeReference.position);
                }
                else
                {
                    _controller.SetPlaneConstraint(false, Vector3.up, Vector3.zero);
                }
            }
            UpdateSecondaryPlaneConstraint();
        }
        

        private void LateUpdate()
        {
            if (_controller == null) return;

            UpdateSecondaryPlaneConstraint();

            _controller.ApplyInput(_queuedInput);
            _queuedInput = FP_OrbitalInput.None;

            _controller.Tick(Time.deltaTime);
            // Emit only when changed (prevents spam / avoids tiny float jitter if you want).
            Quaternion current = _camera.transform.rotation;
            float angle = Quaternion.Angle(_lastRotation, current);
            if (angle >= _rotationEventThresholdDegrees)
            {
                _lastRotation = current;
                var lastFrame = new FP_OrbitalViewState(_lastRotation, localOffCenterCache,0,_camera.orthographic, _camera.orthographicSize);
                OnCameraRotationApplied?.Invoke(lastFrame);
            }
        }

        #region Public Methods
        public void RecenterToTargetBounds(bool fit = true)
        {
            if (_controller == null) return;
            if (TargetBounds == null) return;
            var newCenter = _controller.SetTargetBounds(TargetBounds.bounds, localOffCenterCache,null); // sets pivotTarget = bounds.center
            if (fit) _controller.FitToBoundsForCurrentProjection();
            if (RestrictBelowPlane) UpdateRestrictBelowPlaneFromBounds(newCenter);
            
        }
        /// <summary>
        /// Call after you reset/set TargetBounds if you want the camera to recenter
        /// </summary>
        public void ResetCameraMaxDistance()
        {
            float checkMaxDistance = 0;
            checkMaxDistance = TargetBounds.bounds.size.magnitude * 1.1f;
            _controller.ZoomToFitBounds(checkMaxDistance);
            _controller.FitToBoundsForCurrentProjection();
        }
        public void SetBounds(Bounds b, Vector3 localBoundsOffCenter,Transform optionalFrame = null)
        {
            //update TargetBounds Info
            TargetBounds.size = b.size;
            //TargetBounds.extents = b.extents;
            TargetBounds.center = b.center;
            localOffCenterCache= localBoundsOffCenter;
            var newCenter= _controller.SetTargetBounds(b, localBoundsOffCenter,optionalFrame);
            if (RestrictBelowPlane) UpdateRestrictBelowPlaneFromBounds(newCenter);
        }
           
        public void Snap(FP_OrbitalView view, FP_ProjectionMode mode) =>
            _controller.SnapView(view, mode);

        /// <summary>Called by your app layer (touch, mouse, etc.).</summary>
        public void FeedInput(in FP_OrbitalInput input) => _queuedInput = input;
        #endregion
        /// <summary>
        /// Given the center position we use the extents of the debug bounding box to make adjustments
        /// </summary>
        /// <param name="centerInfo"></param>
        private void UpdateRestrictBelowPlaneFromBounds(Vector3 centerInfo)
        {
            if (!RestrictBelowPlane || _planeReference == null || TargetBounds == null)
                return;

            var lowerPortionFromCenter = TargetBounds.bounds.extents.y;
            Vector3 planePos = new Vector3(
                centerInfo.x,
                centerInfo.y-lowerPortionFromCenter,
                centerInfo.z
            );
            
            _planeReference.position = planePos;
            _planeReference.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            if (_debugPlanes != null)
            {
                var rot = Quaternion.LookRotation(Vector3.forward, _planeReference.up);
                _debugPlanes.DrawPlane(_planeReference.position, rot, new Vector2(10, 10), Color.green, 10f);
            }
            // Push the updated plane into the controller
            _controller.SetPlaneConstraint(
                true,
                _planeReference.up,
                _planeReference.position
            );
        }

        private void UpdateSecondaryPlaneConstraint()
        {
            if (!RestrictSecondaryPlane)
            {
                _controller.SetSecondaryPlaneConstraint(false, Vector3.up, Vector3.zero);
                return;
            }

            Vector3 planeNormal = SecondaryPlaneNormal;
            Vector3 planePoint = planeNormal.normalized * SecondaryPlaneOffset;

            if (_secondaryPlaneReference != null)
            {
                planeNormal = _secondaryPlaneReference.up;
                planePoint = _secondaryPlaneReference.position + planeNormal.normalized * SecondaryPlaneOffset;
            }
            if (_debugPlanes!=null)
            {
                var rot = Quaternion.LookRotation(Vector3.forward, planeNormal);
                _debugPlanes.DrawPlane(_secondaryPlaneReference.position, rot, new Vector2(10, 10), Color.green, 10f);
            }
            _controller.SetSecondaryPlaneConstraint(true, planeNormal, planePoint);
        }

    }
}
