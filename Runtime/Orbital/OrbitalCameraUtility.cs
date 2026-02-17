namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using System;
    [Serializable]
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
    [Serializable]
    public enum FP_OrbitalMouseMode
    {
        None,
        Orbit,
        Pan
    }
    [Serializable]
    public enum FP_ToolbarAction
    {
        None,
        ResetModelPose,
        PrevModel,
        NextModel,
        ToggleVerticesOn,
        ToggleVerticesOff,
        ToggleWireframeOn,
        ToggleWireframeOff,
        ToggleBoundsOn,
        ToggleBoundsOff,
        ToggleRendererOn,
        ToggleRendererOff,
        ToggleMeasurementOn,
        ToggleMeasurementOff,
        ToolMeasureReset,
        OrbitMode,
        PanMode,
        GridXZOn,
        GridXZOff,
        GridXYOn,
        GridXYOff,
        ToolMeasureAngleOn,
        ToolMeasureAngleOff,
        LockCamera,
        UnlockCamera
    }
    [Serializable]
    [System.Flags]
    public enum FPMeshViewFlags
    {
        None = 0,
        Vertices = 1 << 0,
        Wireframe = 1 << 1,
        Renderer = 1 << 2,
        Bounds = 1 << 3,
        Normals = 1 << 4,
        // Surface modes are better as a separate enum, not flags
    }
    [Serializable]
    public enum FPMeasureState
    {
        None,
        WaitingForA,
        WaitingForB,
        Completed
    }
    [Serializable]
    public enum FPMeasureMode
    {
        None,
        Perspective,
        OrthoPlane
    }
    
    [Serializable]
    public enum MeshSurfaceDebugMode
    {
        None,
        WorldNormals,
        UV0,
        VertexColors
    }
    [Serializable]
    public struct PlaneBoundaryDetails
    {
        public bool restrictPlane;
        public Transform planeReference;
        public Vector3 planeNormal;
        public float planeOffset;
        public Vector3 calculatedPoint;
        public FP_OrbitalView planeView;
    }
    /// <summary>
    /// Struct to keep track of mesh view status for runtime mesh viewer
    /// </summary>
    [Serializable]
    public struct FPMeshViewStatus
    {
        public bool ShowRenderer;
        public FPMeshViewFlags Flags;
        public MeshSurfaceDebugMode SurfaceMode;
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

    #region UI Objects
    public enum FP_ViewHomeHit
    {
        NA,
        CameraFrustrum
    }
    public enum FP_ViewCubeHit
    {
        // Faces
        Front,
        Back,
        Left,
        Right,
        Top,
        Bottom,
        // Corners
        TopFrontRight,
        TopFrontLeft,
        TopBackRight,
        TopBackLeft,
        BottomFrontRight,
        BottomFrontLeft,
        BottomBackRight,
        BottomBackLeft,
        // Edges
        TopFront,
        TopBack,
        TopRight,
        TopLeft,
        BottomFront,
        BottomBack,
        BottomRight,
        BottomLeft,
        FrontRight,
        FrontLeft,
        BackRight,
        BackLeft,
    }
    
    public enum FP_ProjectionMode
    {
        None = 0,
        Perspective = 1,
        Orthographic = 2,
    }

    [Serializable]
    public struct FP_ViewPose
    {
        public Vector3 FromDirection;
        public Vector3 UpDirection;

        public FP_ViewPose(Vector3 fromDirection, Vector3 upDirection)
        {
            FromDirection = fromDirection.normalized;
            UpDirection = upDirection.normalized;
        }
        public void NormalizeDirection()
        {
            if (FromDirection.sqrMagnitude > 0f)
            {
                FromDirection.Normalize();
            }
            if (UpDirection.sqrMagnitude > 0f)
            {
                UpDirection.Normalize();
            }
        }
    }

    [Serializable]
    public readonly struct FP_OrbitalViewState
    {
        public readonly Quaternion Rotation;
        public readonly Vector3 Pivot;
        public readonly float Distance;
        public readonly bool IsOrthographic;
        public readonly float OrthoSize;

        public FP_OrbitalViewState(Quaternion rotation, Vector3 pivot, float distance, bool isOrtho, float orthoSize)
        {
            Rotation = rotation;
            Pivot = pivot;
            Distance = distance;
            IsOrthographic = isOrtho;
            OrthoSize = orthoSize;
        }
    }
    public static class FP_ViewCubePoses
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hit"></param>
        /// <returns></returns>
        public static FP_ViewPose Get(FP_ViewCubeHit hit)
        {
            Vector3 up = Vector3.up;

            // Faces
            if (hit == FP_ViewCubeHit.Front) return new FP_ViewPose(new Vector3(0, 0, 1), up);
            if (hit == FP_ViewCubeHit.Back) return new FP_ViewPose(new Vector3(0, 0, -1), up);
            if (hit == FP_ViewCubeHit.Right) return new FP_ViewPose(new Vector3(1, 0, 0), up);
            if (hit == FP_ViewCubeHit.Left) return new FP_ViewPose(new Vector3(-1, 0, 0), up);
            if (hit == FP_ViewCubeHit.Top) return new FP_ViewPose(new Vector3(0, 1, 0), Vector3.forward); // avoid roll
            if (hit == FP_ViewCubeHit.Bottom) return new FP_ViewPose(new Vector3(0, -1, 0), Vector3.forward); // avoid roll

            // Corners
            if (hit == FP_ViewCubeHit.TopFrontRight) return new FP_ViewPose(new Vector3(1, 1, 1), up);
            if (hit == FP_ViewCubeHit.TopFrontLeft) return new FP_ViewPose(new Vector3(-1, 1, 1), up);
            if (hit == FP_ViewCubeHit.TopBackRight) return new FP_ViewPose(new Vector3(1, 1, -1), up);
            if (hit == FP_ViewCubeHit.TopBackLeft) return new FP_ViewPose(new Vector3(-1, 1, -1), up);
            if (hit == FP_ViewCubeHit.BottomFrontRight) return new FP_ViewPose(new Vector3(1, -1, 1), up);
            if (hit == FP_ViewCubeHit.BottomFrontLeft) return new FP_ViewPose(new Vector3(-1, -1, 1), up);
            if (hit == FP_ViewCubeHit.BottomBackRight) return new FP_ViewPose(new Vector3(1, -1, -1), up);
            if (hit == FP_ViewCubeHit.BottomBackLeft) return new FP_ViewPose(new Vector3(-1, -1, -1), up);

            // Edges
            if (hit == FP_ViewCubeHit.TopFront) return new FP_ViewPose(new Vector3(0, 1, 1), up);
            if (hit == FP_ViewCubeHit.TopBack) return new FP_ViewPose(new Vector3(0, 1, -1), up);
            if (hit == FP_ViewCubeHit.TopRight) return new FP_ViewPose(new Vector3(1, 1, 0), up);
            if (hit == FP_ViewCubeHit.TopLeft) return new FP_ViewPose(new Vector3(-1, 1, 0), up);
            if (hit == FP_ViewCubeHit.BottomFront) return new FP_ViewPose(new Vector3(0, -1, 1), up);
            if (hit == FP_ViewCubeHit.BottomBack) return new FP_ViewPose(new Vector3(0, -1, -1), up);
            if (hit == FP_ViewCubeHit.BottomRight) return new FP_ViewPose(new Vector3(1, -1, 0), up);
            if (hit == FP_ViewCubeHit.BottomLeft) return new FP_ViewPose(new Vector3(-1, -1, 0), up);
            if (hit == FP_ViewCubeHit.FrontRight) return new FP_ViewPose(new Vector3(1, 0, 1), up);
            if (hit == FP_ViewCubeHit.FrontLeft) return new FP_ViewPose(new Vector3(-1, 0, 1), up);
            if (hit == FP_ViewCubeHit.BackRight) return new FP_ViewPose(new Vector3(1, 0, -1), up);
            if (hit == FP_ViewCubeHit.BackLeft) return new FP_ViewPose(new Vector3(-1, 0, -1), up);
            
            // Fallback
            return new FP_ViewPose(Vector3.forward, Vector3.up);

            // Top/Bottom we are just swapping UpDirection for forward to avoid roll
        }
    }
    #endregion

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

        [Header("Ortho Zoom Limits")]
        public float OrthoSizeMin = 0.05f;
        public float OrthoSizeMax = 500f;

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
        private FP_ProjectionMode _lastAppliedProjection = FP_ProjectionMode.Perspective;

        // Plane constraints
        private struct PlaneConstraint
        {
            public bool Enabled;
            public Vector3 Normal;
            public float Distance;
        }

        private PlaneConstraint[] _planeConstraints;

        public FP_OrbitalCameraController(Camera camera, FP_OrbitalCameraSettings settings, PlaneBoundaryDetails[] constraints)
        {
            _camera = camera ? camera : throw new ArgumentNullException(nameof(camera));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Clamp();
            _planeConstraints = new PlaneConstraint[constraints.Length];
            for(int i=0; i<constraints.Length; i++)
            {
                _planeConstraints[i] = new PlaneConstraint()
                {
                    Enabled = constraints[i].restrictPlane,
                    Normal = constraints[i].planeNormal,
                    Distance = 0
                };
            }
            _camera.fieldOfView = _settings.DefaultFov;
            _pivot = _pivotTarget = _camera.transform.position;
            _rotation = _rotationTarget = _camera.transform.rotation;
            _distance = _distanceTarget = 5f;
        }
        public void ZoomToFitBounds(float newMaxDistance)
        {
            _settings.DistanceMax = newMaxDistance;
            _settings.Clamp();
        }
        /// <summary>
        /// standard plane equation dot(n, x) + d = 0
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="planeNormal"></param>
        /// <param name="pointOnPlane"></param>
        public void SetPlaneConstraint(bool enabled,Vector3 planeNormal,Vector3 pointOnPlane, int index=0)
        {
            SetPlaneConstraintInternal(index, enabled, planeNormal, pointOnPlane);
        }

        public void SetSecondaryPlaneConstraint(bool enabled, Vector3 planeNormal, Vector3 pointOnPlane)
        {
            SetPlaneConstraintInternal(1, enabled, planeNormal, pointOnPlane);
        }

        private void SetPlaneConstraintInternal(int index, bool enabled, Vector3 planeNormal, Vector3 pointOnPlane)
        {
            if (index < 0 || index >= _planeConstraints.Length)
                return;

            _planeConstraints[index].Enabled = enabled;
            if (!enabled)
                return;

            Vector3 normal = planeNormal.normalized;
            _planeConstraints[index].Normal = normal;
            _planeConstraints[index].Distance = -Vector3.Dot(normal, pointOnPlane);
        }

        /// <summary>
        /// Returns the center value which is the worldBounds.center + centerOffset
        /// </summary>
        /// <param name="worldBounds"></param>
        /// <param name="centerOffset"></param>
        /// <param name="optionalFrame"></param>
        /// <returns></returns>
        public Vector3 SetTargetBounds(Bounds worldBounds, Vector3 centerOffset,Transform optionalFrame = null)
        {
            worldBounds.center += centerOffset;
            _targetBounds = worldBounds;
            _targetFrame = new FP_TargetFrame(optionalFrame);

            _pivotTarget = worldBounds.center;
            if (_pivot == Vector3.zero) _pivot = _pivotTarget;
            return _pivotTarget;
        }
        public void SetProjection(FP_ProjectionMode newProjection)
        {
            _projectionTarget = newProjection;
        }

        /// <summary>
        /// Fits camera zoom (distance or ortho size) to the current target bounds,
        /// using the controller's current projection target.
        /// </summary>
        public void FitToBoundsForCurrentProjection()
        {
            FitToBounds(_projectionTarget);
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
        /// <summary>
        /// UI function to set our camera based on external interfaces
        /// </summary>
        /// <param name="worldRotation"></param>
        /// <param name="projectionMode"></param>
        public void SetRotationTarget(Quaternion worldRotation, FP_ProjectionMode projectionMode, bool fitToBounds=true)
        {
            _projectionTarget = projectionMode;

            _rotationTarget = worldRotation;

            // Keep yaw/pitch stable if you rely on orbit angles
            Vector3 e = _rotationTarget.eulerAngles;
            _yaw = e.y;
            _pitch = NormalizePitch(e.x);

            if (fitToBounds)
            {
                FitToBounds(projectionMode);
            }
            // FitToBounds(...);  // you can pick a conservative fit path here
        }
        public void ChangeCameraPerspective(FP_ProjectionMode newProjection)
        {
            _projectionTarget = newProjection;
        }
        public void ApplyInput(in FP_OrbitalInput input)
        {
            if (input.IsPressed) _isDragging = true;
            if (input.IsReleased) _isDragging = false;

            // Zoom (mouse wheel / pinchDelta)
            if (Mathf.Abs(input.PinchDelta) > Mathf.Epsilon)
            {
                float zoomFactor = 1f - (input.PinchDelta * _settings.ZoomSensitivity);

                // Prevent negative/zero factor from extreme deltas
                zoomFactor = Mathf.Clamp(zoomFactor, 0.05f, 20f);

                // Use projection target (not current camera state) to avoid one-frame lag during mode switches
                bool ortho = (_projectionTarget == FP_ProjectionMode.Orthographic);

                if (ortho)
                {
                    // Ortho zoom = change orthographicSize (smaller = zoom in)
                    _orthoSizeTarget = Mathf.Clamp(_orthoSizeTarget * zoomFactor, _settings.OrthoSizeMin, _settings.OrthoSizeMax);

                    // Optional: keep distance sane for clipping even in ortho
                    // (doesn't affect scale, but avoids near clip issues)
                    _distanceTarget = Mathf.Clamp(_distanceTarget, _settings.DistanceMin, _settings.DistanceMax);
                }
                else
                {
                    // Perspective zoom = change distance
                    _distanceTarget = Mathf.Clamp(_distanceTarget * zoomFactor, _settings.DistanceMin, _settings.DistanceMax);
                }
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
            // Projection switching + transition zoom mapping
            if (_projectionTarget != _lastAppliedProjection)
            {
                // Convert zoom so the view scale stays roughly consistent.
                // Use the CURRENT smoothed values as the starting point (feels best).
                if (_projectionTarget == FP_ProjectionMode.Orthographic)
                {
                    // Going Perspective -> Ortho: compute ortho size that matches current perspective framing
                    float orthoSize = ComputeEquivalentOrthoSizeFromPerspective();
                    _orthoSize = _orthoSizeTarget = Mathf.Clamp(orthoSize, _settings.OrthoSizeMin, _settings.OrthoSizeMax);

                    _camera.orthographic = true;
                    _camera.orthographicSize = _orthoSize;
                }
                else
                {
                    // Going Ortho -> Perspective: compute distance that matches current ortho framing
                    float dist = ComputeEquivalentPerspectiveDistanceFromOrtho();
                    _distance = _distanceTarget = Mathf.Clamp(dist, _settings.DistanceMin, _settings.DistanceMax);

                    _camera.orthographic = false;
                }

                _lastAppliedProjection = _projectionTarget;
            }
            else
            {
                // No transition: keep your existing switching logic
                if (_projectionTarget == FP_ProjectionMode.Perspective)
                {
                    if (_camera.orthographic) _camera.orthographic = false;
                }
                else
                {
                    if (!_camera.orthographic) _camera.orthographic = true;
                }
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
            // Apply transform NEW
            Vector3 desiredCamPos = _pivot + (_rotation * Vector3.back) * _distance;
            if (HasPlaneConstraints() && !IsAboveAllPlanes(desiredCamPos))
            {
                desiredCamPos = ProjectAbovePlanes(desiredCamPos);

                // Optional: gently push the pivot as well for pan cases
                _pivotTarget = Vector3.Lerp(
                    _pivotTarget,
                    desiredCamPos + (_rotation * Vector3.forward) * _distance,
                    0.5f);
            }
            _camera.transform.SetPositionAndRotation(desiredCamPos, _rotation);
            // Apply transform OLD
            /*
            Vector3 camPos = _pivot + (_rotation * Vector3.back) * _distance;
            _camera.transform.SetPositionAndRotation(camPos, _rotation);
        */
        }
        private float ComputeEquivalentOrthoSizeFromPerspective()
        {
            // Ortho size is half of the vertical size visible at the pivot depth.
            // For a perspective camera: halfHeight = distance * tan(fov/2)
            float fovRad = _camera.fieldOfView * Mathf.Deg2Rad;
            float halfHeight = _distance * Mathf.Tan(fovRad * 0.5f);

            // If you want to respect aspect when your content framing is width-bound,
            // you can optionally incorporate it, but halfHeight is usually what people expect.
            return Mathf.Max(_settings.OrthoSizeMin, halfHeight);
        }
        private float ComputeEquivalentPerspectiveDistanceFromOrtho()
        {
            // Inverse of above: distance = orthoSize / tan(fov/2)
            float fovRad = _camera.fieldOfView * Mathf.Deg2Rad;
            float denom = Mathf.Tan(Mathf.Max(0.0001f, fovRad * 0.5f));
            float dist = _orthoSize / denom;

            return Mathf.Max(_settings.DistanceMin, dist);
        }
        private void Orbit(Vector2 dragDelta)
        {
            float yawDelta = dragDelta.x * _settings.OrbitSensitivity;
            float pitchDelta = -dragDelta.y * _settings.OrbitSensitivity;

            float candidateYaw = _yaw + yawDelta;
            float candidatePitch = Mathf.Clamp(
                _pitch + pitchDelta,
                _settings.PitchMin,
                _settings.PitchMax
            );

            // Full candidate rotation (yaw + pitch)
            Quaternion fullCandidate =
                Quaternion.Euler(candidatePitch, candidateYaw, 0f);

            // If full rotation is valid, accept it
            if (!WouldRotationViolatePlane(fullCandidate))
            {
                _yaw = candidateYaw;
                _pitch = candidatePitch;
                _rotationTarget = fullCandidate;
                return;
            }

            // Otherwise, try yaw-only rotation (no pitch change)
            Quaternion yawOnlyCandidate =
                Quaternion.Euler(_pitch, candidateYaw, 0f);

            if (!WouldRotationViolatePlane(yawOnlyCandidate))
            {
                _yaw = candidateYaw;
                _rotationTarget = yawOnlyCandidate;
                // pitch unchanged
            }
            //OLD
            /*
            _yaw += dragDelta.x * _settings.OrbitSensitivity;
            _pitch -= dragDelta.y * _settings.OrbitSensitivity;
            _pitch = Mathf.Clamp(_pitch, _settings.PitchMin, _settings.PitchMax);

            _rotationTarget = Quaternion.Euler(_pitch, _yaw, 0f);
            */
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
            FitToBounds(mode);  
        }
        private void FitToBounds(FP_ProjectionMode mode)
        {
            // Conservative fit using bounds extents projected onto view plane.
            // Works well for axis-aligned bounds; if you need oriented bounds,
            // pass a rotated frame AND provide bounds in that frame (future extension).
            Vector3 ext = _targetBounds.extents;
            float radius = ext.magnitude * _settings.OrthoPadding;

            if (mode == FP_ProjectionMode.Orthographic)
            {
                // Conservative; orientation-agnostic
                float halfHeight = Mathf.Max(ext.y, ext.z, ext.x);
                float halfWidth = halfHeight / Mathf.Max(0.0001f, _camera.aspect);

                _orthoSizeTarget = Mathf.Max(halfHeight, halfWidth) * _settings.OrthoPadding;
                _distanceTarget = Mathf.Clamp(radius * 2.0f, _settings.DistanceMin, _settings.DistanceMax);
            }
            else
            {
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
        private bool IsAbovePlane(Vector3 worldPos)
        {
            // positive means same side as normal
            return IsAboveAllPlanes(worldPos);
        }
        private bool WouldRotationViolatePlane(Quaternion candidateRotation)
        {
            if (!HasPlaneConstraints())
                return false;

            Vector3 candidatePos =
                _pivot + (candidateRotation * Vector3.back) * _distance;

            return !IsAboveAllPlanes(candidatePos);
        }

        private bool HasPlaneConstraints()
        {
            for (int i = 0; i < _planeConstraints.Length; i++)
            {
                if (_planeConstraints[i].Enabled)
                    return true;
            }

            return false;
        }

        private bool IsAboveAllPlanes(Vector3 worldPos)
        {
            for (int i = 0; i < _planeConstraints.Length; i++)
            {
                if (!_planeConstraints[i].Enabled)
                    continue;

                float side = Vector3.Dot(_planeConstraints[i].Normal, worldPos) + _planeConstraints[i].Distance;
                if (side < 0f)
                    return false;
            }

            return true;
        }

        private Vector3 ProjectAbovePlanes(Vector3 worldPos)
        {
            Vector3 adjusted = worldPos;

            for (int i = 0; i < _planeConstraints.Length; i++)
            {
                if (!_planeConstraints[i].Enabled)
                    continue;

                float side = Vector3.Dot(_planeConstraints[i].Normal, adjusted) + _planeConstraints[i].Distance;
                if (side < 0f)
                {
                    adjusted -= _planeConstraints[i].Normal * side;
                }
            }

            return adjusted;
        }


    }
}
