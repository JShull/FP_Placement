namespace FuzzPhyte.Placement.Interaction
{
    using UnityEngine;
    using System;
    using UnityEngine.Events;
    [Serializable] public class PlacementInteractionEvent : UnityEvent<PlacementObject,FP_PlacementSocketComponent> { }
    public class FP_PlacementInteractionBehaviour : PlacementBaseInput
    {
        [Header("Placement")]
        [SerializeField] private float maxRayDistance = 100f;
        [SerializeField] protected bool drawDebug = false;
        [SerializeField] private PlacementObject _activePlacement;
        [SerializeField] private PlacementObjectComponent _activeComponent;
        private FP_PlacementSocketComponent _activeSocket;

        [SerializeField] private Vector3 _startPos;
        [SerializeField] private Quaternion _startRot;
        [Space]
        [Header("Drag Surface Parameters")]
        [SerializeField] private float boxCastDist = 2f;
        [SerializeField] private LayerMask surfaceMask;
        [SerializeField] private float surfaceCastDistance = 2f;

        private Transform _currentSurface;
        private Plane _currentSurfacePlane;
        [Header("Private Parameters")]
        [SerializeField] private float _dragRayDistance;
        [SerializeField] private Vector3 _dragLocalOffset;
        [SerializeField] private Transform _dragTarget;
        private FP_PlacementSocketComponent _hoverSocket;
        private FP_PlacementSocketComponent _previousHoverSocket;
        private FP_PlacementSocketComponent newHover;

        [Header("Placement Bounds")]
        [SerializeField] private bool enforceBounds = true;
        [SerializeField] private Vector3 boundsCenter = Vector3.zero;
        [SerializeField] private Vector3 boundsSize = new Vector3(10f, 5f, 10f);


        [Header("Clicky Placement Events")]
        [SerializeField] protected PlacementInteractionEvent doubleClickEvent;
        [SerializeField] protected PlacementInteractionEvent singleClickEvent;
        [SerializeField] protected PlacementInteractionEvent dragEndSocketSuccessEvent;
        [SerializeField] protected PlacementInteractionEvent dragEndSocketFailedEvent;
        [SerializeField] protected PlacementInteractionEvent dragEndMovedLocationEvent;
        public override void OnEnable()
        {
            base.OnEnable();
        }
        public override void OnDisable()
        {
            base.OnDisable();
        }
        protected override void UpdateLogic()
        {
            ResolveClickIfNeeded();
            // Pointer position already validated + gated by base class
            Vector2 screenPos = _pointerPosition.action.ReadValue<Vector2>();
            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            // --- Begin drag ---
            if (_startedThisFrame)
            {
                BeginDrag(ray);
            }
            // --- Update drag ---
            if (_isDown && _activePlacement != null)
            {
                UpdateDrag(ray);
            }
            // --- End drag ---
            if (_releasedThisFrame)
            {
                EndDrag();
            }
            // reset per-frame flags (important!)
            _startedThisFrame = false;
            _releasedThisFrame = false;
        }
        #region Drag Related Logic
        protected void BeginDrag(Ray ray)
        {
            if (Physics.Raycast(ray, out var hit, maxRayDistance))
            {
                if (drawDebug)
                {
                    Debug.DrawRay(ray.origin, ray.direction*maxRayDistance, Color.red, 2f);
                }

                if (hit.collider.TryGetComponent(out PlacementObjectComponent poc))
                {
                    if (poc.Locked) return;

                    _activeComponent = poc;
                    _activePlacement = poc.PlacementData;
                    _startPos = poc.RootPlacement.position;
                    _startRot = poc.RootPlacement.rotation;
                    _dragTarget = poc.RootPlacement;

                    // current socket check and remove it if we have one
                    if (_activeComponent.CurrentSocket != null)
                    {
                        _activeComponent.CurrentSocket.RemovePlacement(_activePlacement);
                        _activeComponent.CurrentSocket = null;
                    }
                    // plane work
                    // Where did we hit the object?
                    Plane dragPlane = new Plane(-ray.direction, _dragTarget.position);
                    dragPlane.Raycast(ray, out _dragRayDistance);

                    // Offset so we don’t snap pivot to cursor
                    Vector3 hitPoint = ray.GetPoint(_dragRayDistance);
                    _dragLocalOffset = _dragTarget.position - hitPoint;
                }
            }
        }
       
        protected void UpdateDrag(Ray ray)
        {
            if (_activePlacement == null || _dragTarget == null)
                return;

            if (drawDebug)
            {
                Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, Color.orange, 0.5f);
            }

            // 1. Always update free drag first
            UpdateFreeDrag(ray);

            // 2. Check for socket hover
            UpdateSocketHover(ray);

            // 3. If hovering a valid socket, override transform
            if (_hoverSocket != null)
            {
                ApplySocketOverride();
                _activeSocket = _hoverSocket;
            }
            else
            {
                _activeSocket = null;
            }
            return;

        }
        #region Drag Additional
     
        protected void UpdateFreeDrag(Ray ray)
        {
            if (_dragTarget == null || _activeComponent == null)
                return;

            // 1. Find surface below the dragged object
            ResolveSurfaceBelow();

            // 2. Project mouse onto surface plane
            Vector3 targetPoint = GetSurfaceProjectedPoint(ray);

            // 3. Maintain bottom contact
            targetPoint = ApplySurfaceHeightCorrection(targetPoint);

            _dragTarget.position = targetPoint;
        }
        protected void ResolveSurfaceBelow()
        {
            var sides = _activeComponent.Sides.ToArray();
            FP_PlacementSide bottom = null;

            foreach (var side in sides)
            {
                if (side.SideType == FPObjectSideType.Bottom)
                {
                    bottom = side;
                    break;
                }
            }

            if (bottom == null)
                return;

            Vector3 origin = bottom.transform.position + Vector3.up * 0.1f;
            Vector3 halfExtents = new Vector3(
                bottom.SurfaceSize.x * 0.5f,
                0.05f,
                bottom.SurfaceSize.y * 0.5f
            );

            if (Physics.BoxCast(
                origin,
                halfExtents,
                Vector3.down,
                out RaycastHit hit,
                bottom.transform.rotation,
                surfaceCastDistance,
                surfaceMask))
            {
                _currentSurface = hit.collider.transform;
                _currentSurfacePlane = new Plane(hit.normal, hit.point);
            }
        }
        protected Vector3 GetSurfaceProjectedPoint(Ray ray)
        {
            if (_currentSurface == null)
            {
                // fallback to original camera depth
                return ray.GetPoint(_dragRayDistance) + _dragLocalOffset;
            }

            if (_currentSurfacePlane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter) + _dragLocalOffset;
            }

            return _dragTarget.position;
        }
        Vector3 ApplySurfaceHeightCorrection(Vector3 targetPoint)
        {
            var sides = _activeComponent.Sides.ToArray();
            FP_PlacementSide bottom = null;

            foreach (var side in sides)
            {
                if (side.SideType == FPObjectSideType.Bottom)
                {
                    bottom = side;
                    break;
                }
            }

            if (bottom == null)
                return targetPoint;

            float bottomOffset =
                bottom.transform.position.y - _dragTarget.position.y;

            targetPoint.y -= bottomOffset;
            return targetPoint;
        }
        void UpdateSocketHover(Ray ray)
        {
            newHover = null;

            var hits = Physics.RaycastAll(ray, maxRayDistance, placementMask);

            if (hits == null || hits.Length == 0)
                return;

            foreach (var hit in hits)
            {
                // Socket might be anywhere in the hierarchy
                if (!hit.collider.TryGetComponent(out FP_PlacementSocketComponent socket))
                    continue;

                if (drawDebug)
                {
                    Debug.Log($"[Placement] Ray hit socket candidate: {socket.name}");
                }

                if (!socket.CanAccept(_activePlacement))
                    continue;

                newHover = socket;
                break; // first valid socket wins break out
            }
            if (_previousHoverSocket != newHover)
            {
                if (_previousHoverSocket != null)
                    _previousHoverSocket.SetHoverState(false);

                if (newHover != null)
                    newHover.SetHoverState(true);

                _previousHoverSocket = newHover;
            }
            _hoverSocket = newHover;
        }
        void ApplySocketOverride()
        {
            if (_hoverSocket == null || _activeComponent == null)
                return;

            Transform t = _activeComponent.RootPlacement;

            t.SetPositionAndRotation(
                _hoverSocket.transform.position,
                _hoverSocket.transform.rotation
            );
        }
        #endregion
        protected bool TryFindSurfaceBelow(out RaycastHit hit)
        {
            var movingObj = _activeComponent;
            var surfaceMask = placementMask;
            var bottomSide = movingObj.GetSide(FPObjectSideType.Bottom);

            Vector3 origin = bottomSide.transform.position + Vector3.up * 0.1f;
            Vector3 halfExtents = new Vector3(
                bottomSide.SurfaceSize.x * 0.5f,
                0.05f,
                bottomSide.SurfaceSize.y * 0.5f
            );

            return Physics.BoxCast(
                origin,
                halfExtents,
                Vector3.down,
                out hit,
                bottomSide.transform.rotation,
                boxCastDist,
                surfaceMask
            );
        }
        protected Vector3 GetDragSurfacePoint(Plane plane, Vector2 curMousePoint)
        {
            var cam = targetCamera;
            Ray ray = cam.ScreenPointToRay(curMousePoint);

            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return Vector3.zero;
        }
        protected void EndDrag()
        {
            if (_activePlacement == null)
                return;

            if (_activeSocket != null)
            {
                _activeSocket.CommitPlacement(
                    _activePlacement,
                    _activeComponent.transform
                );
                _activeComponent.CurrentSocket = _activeSocket;
                dragEndSocketSuccessEvent?.Invoke(_activePlacement, _activeSocket);
            }
            else
            {
                //No socket : surface drop allowed
                Vector3 dropPos = _activeComponent.transform.position;

                if (!IsWithinBounds(dropPos))
                {
                    // Out of bounds → return home
                    _activeComponent.transform.SetPositionAndRotation(_startPos, _startRot);
                }
                else
                {
                    // In bounds → keep dropped position
                    if (drawDebug)
                    {
                        Debug.Log("[Placement] Dropped on surface (no socket). Keeping position.");
                    }
                    dragEndMovedLocationEvent?.Invoke(_activePlacement, null);
                }
                dragEndSocketFailedEvent?.Invoke(_activePlacement, null);
            }
            if(_previousHoverSocket != null)
            {
                _previousHoverSocket.SetHoverState(false);
            }
            _activePlacement = null;
            _activeComponent = null;
            _activeSocket = null;
            _hoverSocket = null;
            _dragTarget = null;
            _previousHoverSocket = null;
        }
        protected bool IsWithinBounds(Vector3 position)
        {
            if (!enforceBounds)
                return true;

            Bounds b = new Bounds(boundsCenter, boundsSize);
            return b.Contains(position);
        }
        private void OnDrawGizmosSelected()
        {
            if (!enforceBounds)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }

        #endregion
        /// <summary>
        /// What we want to do via Double Click
        /// </summary>
        /// <param name="worldPos"></param>
        protected override void OnPrimaryDoubleClick(Vector3 worldPos)
        {
            doubleClickEvent?.Invoke(_activePlacement, _activeSocket);
        }
        protected override void OnPrimaryClick(Vector3 worldPos)
        {
            singleClickEvent?.Invoke(_activePlacement,_activeSocket);
        }
    }
}
