namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using System;
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

        public FP_OrbitalCameraController Controller => _controller;
        public BoxCollider TargetBounds;
        [Header("Rotation Event Parameters")]
        [SerializeField] private float _rotationEventThresholdDegrees = 0.05f;
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
                _controller.SetTargetBounds(TargetBounds.bounds, null);
                checkMaxDistance = TargetBounds.bounds.size.magnitude*1.1f;
                _controller.ZoomToFitBounds(checkMaxDistance);
            } 
        }
        

        private void LateUpdate()
        {
            if (_controller == null) return;

            _controller.ApplyInput(_queuedInput);
            _queuedInput = FP_OrbitalInput.None;

            _controller.Tick(Time.deltaTime);
            // Emit only when changed (prevents spam / avoids tiny float jitter if you want).
            Quaternion current = _camera.transform.rotation;
            float angle = Quaternion.Angle(_lastRotation, current);
            if (angle >= _rotationEventThresholdDegrees)
            {
                _lastRotation = current;
                var lastFrame = new FP_OrbitalViewState(_lastRotation, Vector3.zero,0,_camera.orthographic, _camera.orthographicSize);
                OnCameraRotationApplied?.Invoke(lastFrame);
            }
        }

        #region Public Methods
        public void RecenterToTargetBounds(bool fit = true)
        {
            if (_controller == null) return;
            if (TargetBounds == null) return;
            _controller.SetTargetBounds(TargetBounds.bounds, null); // sets pivotTarget = bounds.center
            if (fit) _controller.FitToBoundsForCurrentProjection();
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
        public void SetBounds(Bounds b, Transform optionalFrame = null) =>
            _controller.SetTargetBounds(b, optionalFrame);

        public void Snap(FP_OrbitalView view, FP_ProjectionMode mode) =>
            _controller.SnapView(view, mode);

        /// <summary>Called by your app layer (touch, mouse, etc.).</summary>
        public void FeedInput(in FP_OrbitalInput input) => _queuedInput = input;
        #endregion
    }
}
