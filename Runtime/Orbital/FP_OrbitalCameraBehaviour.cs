namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using System;
    using FuzzPhyte.Utility;
    using System.Collections.Generic;
    
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
        //[Header("Plane Constraint")]
        //public bool RestrictBelowPlane = false;
        [Tooltip("Use the bounds information to move the plane to align")]
        public bool MovePlaneBasedOnBounds = true;
        private Transform _belowPlaneReference;
        //public Vector3 BelowPlaneNormal = Vector3.up;
        //public float BelowPlaneOffset = 0f;
        [Tooltip("If we have any planes restricted, we are restricted")]
        private bool restrictedCamera = false;
        public bool CameraRestricted => restrictedCamera;
        [Space]
        [Header("Plane Boundaries")]
        public List<PlaneBoundaryDetails> PlaneBoundaryDetails = new List<PlaneBoundaryDetails>();
        //[Space]
        //[Header("Secondary Plane Constraint")]
        //public bool RestrictSecondaryPlane = false;
       // [SerializeField] public Transform _secondaryPlaneReference;
       // public Vector3 SecondaryPlaneNormal = Vector3.up;
       // public float SecondaryPlaneOffset = 0f;
        [Header("Plane Debugging")]
        [SerializeField] FP_UtilityDraw _debugPlanes;
        [SerializeField] bool drawDebugPlanesGizmos = false;
        [SerializeField] bool drawDebugOnSelectOnly = true;
        [SerializeField] float planeSize = 5;
        /// <summary>
        /// Fired after the camera transform has been applied (LateUpdate).
        /// Subscribers can sync UI (e.g., ViewCube) to match the camera view.
        /// </summary>
        public event Action<FP_OrbitalViewState> OnCameraRotationApplied;

        private Quaternion _lastRotation;

        private void Awake()
        {
            if (!_camera) _camera = GetComponentInChildren<Camera>();
            UpdatePlaneDetails();// build the PlaneBoundaryDetails Points
            _controller = new FP_OrbitalCameraController(_camera, _settings, PlaneBoundaryDetails.ToArray());
            //once more but now with an active _controller
            UpdatePlaneDetails();
            if (TargetBounds != null)
            {
                float checkMaxDistance = 0;
                var newCenter = _controller.SetTargetBounds(TargetBounds.bounds, Vector3.zero,null);
                checkMaxDistance = TargetBounds.bounds.size.magnitude*1.1f;
                _controller.ZoomToFitBounds(checkMaxDistance);
                if (MovePlaneBasedOnBounds)
                {
                    //just move the first one?
                    UpdateRestrictBelowPlaneFromBounds(newCenter);
                }
            }
            
            /// this now sets all of our planes based on the data
            /*
            for(int i=0;i< PlaneBoundaryDetails.Count; i++)
            {
                var curPlaneDetails = PlaneBoundaryDetails[i];
                _controller.SetPlaneConstraint(curPlaneDetails.restrictPlane, curPlaneDetails.planeNormal, curPlaneDetails.calculatedPoint, i);
            }
            */
        }
        
        private void LateUpdate()
        {
            if (_controller == null) return;
            //update our plane constraints if we moved them?
            UpdatePlaneDetails();

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
            if (MovePlaneBasedOnBounds)
            {
                UpdateRestrictBelowPlaneFromBounds(newCenter);
            }
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
            if (MovePlaneBasedOnBounds)
            {
                UpdateRestrictBelowPlaneFromBounds(newCenter);
            }
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
            if ( _belowPlaneReference == null || TargetBounds == null)
                return;

            var lowerPortionFromCenter = TargetBounds.bounds.extents.y;
            Vector3 planePos = new Vector3(
                centerInfo.x,
                centerInfo.y-lowerPortionFromCenter,
                centerInfo.z
            );
            
            _belowPlaneReference.position = planePos;
            _belowPlaneReference.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            if (_debugPlanes != null)
            {
                var rot = Quaternion.LookRotation(Vector3.forward, _belowPlaneReference.up);
                _debugPlanes.DrawPlane(_belowPlaneReference.position, rot, new Vector2(planeSize, planeSize), Color.yellow, 10f);
            }
            // Push the updated plane into the controller
            _controller.SetPlaneConstraint(
                true,
                _belowPlaneReference.up,
                _belowPlaneReference.position,
                0
            );
        }
        private void UpdatePlaneDetails()
        {
            if (PlaneBoundaryDetails.Count > 0)
            {
                _belowPlaneReference = PlaneBoundaryDetails[0].planeReference;
            }
            bool restricted = false;
            for (int i = 0; i < PlaneBoundaryDetails.Count; i++)
            {
                var curPlane = PlaneBoundaryDetails[i];
                if (curPlane.restrictPlane)
                {
                    //the moment we have one restricted plane = we are restricted
                    restricted = true;
                }
                curPlane.calculatedPoint = ReturnPlanePoint(curPlane.planeReference.position, curPlane.planeNormal, curPlane.planeOffset, curPlane.planeReference);
                if (_controller != null)
                {
                    _controller.SetPlaneConstraint(curPlane.restrictPlane, curPlane.planeNormal, curPlane.calculatedPoint, i);
                }
                if (_debugPlanes != null)
                {
                    //var rot = Quaternion.LookRotation(Vector3.forward, curPlane.planeNormal);
                    var rot = Quaternion.FromToRotation(Vector3.up, curPlane.planeNormal);

                    _debugPlanes.DrawPlane(curPlane.calculatedPoint, rot, new Vector2(planeSize, planeSize), Color.yellow, 1f);
                }
            }
            restrictedCamera = restricted;
            
        }
        private void OnDrawGizmosSelected()
        {
           
            if (!drawDebugPlanesGizmos) return;
            for (int i = 0; i < PlaneBoundaryDetails.Count; i++)
            {
                var curPlane = PlaneBoundaryDetails[i];
                
                curPlane.calculatedPoint = ReturnPlanePoint(curPlane.planeReference.position, curPlane.planeNormal, curPlane.planeOffset, curPlane.planeReference);
                
                if (_debugPlanes != null)
                {
                    //var rot = Quaternion.LookRotation(Vector3.forward, curPlane.planeNormal);
                    var rot = Quaternion.FromToRotation(Vector3.up, curPlane.planeNormal);

                    _debugPlanes.DrawPlane(curPlane.calculatedPoint, rot, new Vector2(planeSize, planeSize), Color.yellow, 1f);
                }
            }
        }
        private void OnDrawGizmos()
        {
            if (drawDebugOnSelectOnly) return;
            if (!drawDebugPlanesGizmos) return;
            for (int i = 0; i < PlaneBoundaryDetails.Count; i++)
            {
                var curPlane = PlaneBoundaryDetails[i];

                curPlane.calculatedPoint = ReturnPlanePoint(curPlane.planeReference.position, curPlane.planeNormal, curPlane.planeOffset, curPlane.planeReference);

                if (_debugPlanes != null)
                {
                    //var rot = Quaternion.LookRotation(Vector3.forward, curPlane.planeNormal);
                    var rot = Quaternion.FromToRotation(Vector3.up, curPlane.planeNormal);

                    _debugPlanes.DrawPlane(curPlane.calculatedPoint, rot, new Vector2(planeSize, planeSize), Color.cyan, 1f);
                }
            }
        }


        /*
        private void UpdateSecondaryPlaneConstraint()
        {
            if (!RestrictSecondaryPlane)
            {
                _controller.SetSecondaryPlaneConstraint(false, Vector3.up, Vector3.zero);
                return;
            }

            Vector3 planeNormal = SecondaryPlaneNormal;
            Vector3 planePoint = SecondaryPlaneNormal.normalized * SecondaryPlaneOffset;
            Vector3 refForward = Vector3.forward;
            if (_secondaryPlaneReference != null)
            {
                refForward = _secondaryPlaneReference.forward;
                planeNormal = _secondaryPlaneReference.up;
                planePoint = _secondaryPlaneReference.position + planeNormal.normalized * SecondaryPlaneOffset;
            }
            if (_debugPlanes != null)
            {
                Vector3 planeForward = Vector3.ProjectOnPlane(refForward, planeNormal).normalized;
                var rot = Quaternion.LookRotation(planeForward, planeNormal);

                _debugPlanes.DrawPlane(planePoint, rot, new Vector2(planeSize, planeSize), Color.green, 10f);
            }
            _controller.SetSecondaryPlaneConstraint(true, planeNormal, planePoint);
        }
        */
        private Vector3 ReturnPlanePoint(Vector3 pos, Vector3 normal, float planeOffset, Transform worldPlane)
        {
            Vector3 planeNormal = normal.normalized;
            Vector3 planePoint = planeNormal * planeOffset;
            Vector3 refForward = Vector3.forward;
            if (worldPlane != null)
            {
                refForward = worldPlane.forward;
                planeNormal = worldPlane.up;
                planePoint = worldPlane.position + planeNormal * planeOffset;
            }
            return planePoint;
        }
        private void PlaneBoundaryConstraint(PlaneBoundaryDetails details)
        {
            if (!details.restrictPlane)
            {
                return;
            }

        }
        private void UpdateRestrictBelowPlane(Vector3 passedPlaneNormal,float planeOffset)
        {
            
            Vector3 planeNormal = passedPlaneNormal;
            Vector3 planePoint = planeNormal.normalized * planeOffset;

            if (_belowPlaneReference != null)
            {
                planeNormal = _belowPlaneReference.up;
                planePoint = _belowPlaneReference.position + planeNormal.normalized * planeOffset;
            }
            if (_debugPlanes != null)
            {
                var rot = Quaternion.LookRotation(Vector3.forward, planeNormal);
                _debugPlanes.DrawPlane(planePoint, rot, new Vector2(planeSize, planeSize), Color.green, 10f);
            }
            _controller.SetPlaneConstraint(true, planeNormal, planePoint);
        }

    }
}
