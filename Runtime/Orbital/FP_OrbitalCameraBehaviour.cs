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

        private void Awake()
        {
            if (!_camera) _camera = GetComponentInChildren<Camera>();
            _controller = new FP_OrbitalCameraController(_camera, _settings);
        }

        private void LateUpdate()
        {
            if (_controller == null) return;

            _controller.ApplyInput(_queuedInput);
            _queuedInput = FP_OrbitalInput.None;

            _controller.Tick(Time.deltaTime);
        }

        public void SetBounds(Bounds b, Transform optionalFrame = null) =>
            _controller.SetTargetBounds(b, optionalFrame);

        public void Snap(FP_OrbitalView view, FP_ProjectionMode mode) =>
            _controller.SnapView(view, mode);

        /// <summary>Called by your app layer (touch, mouse, etc.).</summary>
        public void FeedInput(in FP_OrbitalInput input) => _queuedInput = input;
    }
}
