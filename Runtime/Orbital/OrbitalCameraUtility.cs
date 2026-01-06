namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using System;
    public enum FP_OrbitalView
    {
        None = 0,
        Front=1,
        Back=2,
        Left=3,
        Right=4,
        Top=5,
        Bottom=6
    }
    public enum FP_ProjectionMode
    {
        None=0,
        Perspective=1,
        Orthographic=2,
    }

    public readonly struct FP_OrbitalInput
    {
        public readonly bool IsPressed;
        public readonly bool IsReleased;
        public readonly Vector2 PointerPos;     // screen pixels if you want; optional usage
        public readonly Vector2 DragDelta;      // pixels or normalized; caller decides
        public readonly float PinchDelta;       // positive = zoom in, negative = zoom out
        public readonly bool IsTwoFinger;       // optional hint from caller

        public FP_OrbitalInput(
            bool isPressed,
            bool isReleased,
            Vector2 pointerPos,
            Vector2 dragDelta,
            float pinchDelta,
            bool isTwoFinger)
        {
            IsPressed = isPressed;
            IsReleased = isReleased;
            PointerPos = pointerPos;
            DragDelta = dragDelta;
            PinchDelta = pinchDelta;
            IsTwoFinger = isTwoFinger;
        }

        public static FP_OrbitalInput None => new FP_OrbitalInput(false, false, default, default, 0f, false);

    }
    [Serializable]
    public sealed class FP_OrbitalCameraSettings
    {
        [Header("Orbit")]
        public float OrbitSensitivity = 0.15f;          // degrees per pixel (if pixel deltas)
        public float PitchMin = -85f;
        public float PitchMax = 85f;

        [Header("Pan")]
        public float PanSensitivity = 0.0025f;          // world units per pixel * distance scale
        public bool PanUsesCameraPlane = true;          // pan along camera right/up

        [Header("Zoom")]
        public float ZoomSensitivity = 0.05f;           // scale on pinch delta
        public float DistanceMin = 0.25f;
        public float DistanceMax = 250f;

        [Header("Damping")]
        public float PositionLerp = 14f;
        public float RotationLerp = 14f;
        public float ZoomLerp = 18f;

        [Header("Ortho")]
        public float OrthoPadding = 1.10f;              // >1 means extra margin around bounds
        public float DefaultFov = 45f;

        public void Clamp()
        {
            DistanceMin = Mathf.Max(0.01f, DistanceMin);
            DistanceMax = Mathf.Max(DistanceMin, DistanceMax);
            PitchMin = Mathf.Clamp(PitchMin, -89.9f, 0f);
            PitchMax = Mathf.Clamp(PitchMax, 0f, 89.9f);
            OrthoPadding = Mathf.Max(1f, OrthoPadding);
        }
    }
    /// <summary>
    /// Defines how "front/top/right" are interpreted for the target volume.
    /// If Frame is null, world axes are used (Vector3.forward/up/right).
    /// </summary>
    public readonly struct FP_TargetFrame
    {
        public readonly Transform Frame; // optional

        public FP_TargetFrame(Transform frame)
        {
            Frame = frame;
        }

        public Vector3 Forward => Frame ? Frame.forward : Vector3.forward;
        public Vector3 Up => Frame ? Frame.up : Vector3.up;
        public Vector3 Right => Frame ? Frame.right : Vector3.right;
    }

    /// <summary>
    /// Core orbital camera brain. Does not read input directly.
    /// You feed it bounds and input packets; it outputs transforms onto a Camera.
    /// </summary>
    public sealed class FP_OrbitalCameraController
    {
        private readonly Camera _camera;
        private readonly FP_OrbitalCameraSettings _settings;

        private Bounds _targetBounds;
        private FP_TargetFrame _targetFrame;

        // Desired state
        private Vector3 _pivotTarget;
        private Quaternion _rotationTarget = Quaternion.identity;
        private float _distanceTarget = 5f;
        private FP_ProjectionMode _projectionTarget = FP_ProjectionMode.Perspective;
        private float _orthoSizeTarget = 5f;

        // Smoothed state
        private Vector3 _pivot;
        private Quaternion _rotation = Quaternion.identity;
        private float _distance = 5f;
        private float _orthoSize = 5f;

        // Orbit angles for stable clamping
        private float _yaw;
        private float _pitch;

        // Gesture state (optional)
        private bool _isDragging;

        public FP_OrbitalCameraController(Camera camera, FP_OrbitalCameraSettings settings)
        {
            _camera = camera ? camera : throw new ArgumentNullException(nameof(camera));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Clamp();

            _camera.fieldOfView = _settings.DefaultFov;
            _pivot = _pivotTarget = _camera.transform.position;
            _rotation = _rotationTarget = _camera.transform.rotation;
            _distance = _distanceTarget = 5f;
        }

        public void SetTargetBounds(Bounds worldBounds, Transform optionalFrame = null)
        {
            _targetBounds = worldBounds;
            _targetFrame = new FP_TargetFrame(optionalFrame);

            _pivotTarget = worldBounds.center;
            if (_pivot == Vector3.zero) _pivot = _pivotTarget;
        }

        /// <summary>
        /// Rotates and positions the camera to observe the target bounds from a canonical direction.
        /// This method never modifies the target, bounds, or world transforms.
        /// </summary>
        public void SnapView(FP_OrbitalView view, FP_ProjectionMode projectionMode)
        {
            _projectionTarget = projectionMode;

            // Determine look direction based on target frame.
            // Camera looks *toward* pivot, so we place it along -lookDir from pivot.
            Vector3 forward = _targetFrame.Forward;
            Vector3 up = _targetFrame.Up;
            Vector3 right = _targetFrame.Right;

            Vector3 lookDir = view switch
            {
                FP_OrbitalView.Front => forward,
                FP_OrbitalView.Back => -forward,
                FP_OrbitalView.Right => right,
                FP_OrbitalView.Left => -right,
                FP_OrbitalView.Top => up,
                FP_OrbitalView.Bottom => -up,
                _ => forward
            };

            // Choose a consistent "up" for the view to avoid roll.
            // For top/bottom, use forward as up reference; otherwise use frame up.
            Vector3 upRef = (view == FP_OrbitalView.Top || view == FP_OrbitalView.Bottom) ? forward : up;
            _rotationTarget = Quaternion.LookRotation(-lookDir, upRef);

            // Update yaw/pitch from target rotation to keep orbit math consistent
            Vector3 e = _rotationTarget.eulerAngles;
            _yaw = e.y;
            _pitch = NormalizePitch(e.x);

            // Fit distance / ortho size to bounds
            FitToBounds(view, projectionMode);
        }

        public void ApplyInput(in FP_OrbitalInput input)
        {
            if (input.IsPressed) _isDragging = true;
            if (input.IsReleased) _isDragging = false;

            // Zoom (pinch)
            if (Mathf.Abs(input.PinchDelta) > Mathf.Epsilon)
            {
                float zoomFactor = 1f - (input.PinchDelta * _settings.ZoomSensitivity);
                _distanceTarget = Mathf.Clamp(_distanceTarget * zoomFactor, _settings.DistanceMin, _settings.DistanceMax);
            }

            if (!_isDragging) return;

            // Two-finger drag often maps to pan; one-finger drag maps to orbit.
            if (input.IsTwoFinger)
            {
                Pan(input.DragDelta);
            }
            else
            {
                Orbit(input.DragDelta);
            }
        }

        public void Tick(float deltaTime)
        {
            // Projection switching
            if (_projectionTarget == FP_ProjectionMode.Perspective)
            {
                if (_camera.orthographic) _camera.orthographic = false;
            }
            else
            {
                if (!_camera.orthographic) _camera.orthographic = true;
            }

            // Smooth state toward targets
            float posT = 1f - Mathf.Exp(-_settings.PositionLerp * deltaTime);
            float rotT = 1f - Mathf.Exp(-_settings.RotationLerp * deltaTime);
            float zoomT = 1f - Mathf.Exp(-_settings.ZoomLerp * deltaTime);

            _pivot = Vector3.Lerp(_pivot, _pivotTarget, posT);
            _rotation = Quaternion.Slerp(_rotation, _rotationTarget, rotT);
            _distance = Mathf.Lerp(_distance, _distanceTarget, zoomT);
            _orthoSize = Mathf.Lerp(_orthoSize, _orthoSizeTarget, zoomT);

            if (_camera.orthographic)
                _camera.orthographicSize = _orthoSize;

            // Apply transform
            Vector3 camPos = _pivot + (_rotation * Vector3.back) * _distance;
            _camera.transform.SetPositionAndRotation(camPos, _rotation);
        }

        private void Orbit(Vector2 dragDelta)
        {
            _yaw += dragDelta.x * _settings.OrbitSensitivity;
            _pitch -= dragDelta.y * _settings.OrbitSensitivity;
            _pitch = Mathf.Clamp(_pitch, _settings.PitchMin, _settings.PitchMax);

            _rotationTarget = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void Pan(Vector2 dragDelta)
        {
            // Scale pan by distance so it feels stable at different zoom levels.
            float scale = _settings.PanSensitivity * Mathf.Max(0.1f, _distanceTarget);

            Vector3 right = _camera.transform.right;
            Vector3 up = _camera.transform.up;

            Vector3 deltaWorld = (-right * dragDelta.x + -up * dragDelta.y) * scale;
            _pivotTarget += deltaWorld;
        }

        private void FitToBounds(FP_OrbitalView view, FP_ProjectionMode mode)
        {
            // Conservative fit using bounds extents projected onto view plane.
            // Works well for axis-aligned bounds; if you need oriented bounds,
            // pass a rotated frame AND provide bounds in that frame (future extension).

            Vector3 ext = _targetBounds.extents;
            float radius = ext.magnitude;
            radius *= _settings.OrthoPadding;

            if (mode == FP_ProjectionMode.Orthographic)
            {
                // Ortho size is "half-height" in world units.
                // Use the larger of (height, width) scaled by aspect.
                float halfHeight = Mathf.Max(ext.y, ext.z, ext.x);
                float halfWidth = halfHeight / Mathf.Max(0.0001f, _camera.aspect);
                _orthoSizeTarget = Mathf.Max(halfHeight, halfWidth) * _settings.OrthoPadding;

                // Distance still matters for clipping; keep a sane offset.
                _distanceTarget = Mathf.Clamp(radius * 2.0f, _settings.DistanceMin, _settings.DistanceMax);
            }
            else
            {
                // Fit distance for perspective using fov (approx sphere fit).
                float fovRad = _camera.fieldOfView * Mathf.Deg2Rad;
                float dist = radius / Mathf.Sin(Mathf.Max(0.0001f, fovRad * 0.5f));
                _distanceTarget = Mathf.Clamp(dist, _settings.DistanceMin, _settings.DistanceMax);
            }
        }

        private static float NormalizePitch(float pitchEulerX)
        {
            // Unity returns 0..360; convert to -180..180 then clamp.
            if (pitchEulerX > 180f) pitchEulerX -= 360f;
            return pitchEulerX;
        }
    }
}
