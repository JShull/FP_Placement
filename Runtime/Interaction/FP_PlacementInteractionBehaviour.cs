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

        [Header("Private Parameters")]
        [SerializeField] private float _dragRayDistance;
        [SerializeField] private Vector3 _dragLocalOffset;
        [SerializeField] private Transform _dragTarget;
        private FP_PlacementSocketComponent _hoverSocket;
        private FP_PlacementSocketComponent _previousHoverSocket;
        private FP_PlacementSocketComponent newHover;


        [Header("Clicky Placement Events")]
        [SerializeField] protected PlacementInteractionEvent doubleClickEvent;
        [SerializeField] protected PlacementInteractionEvent singleClickEvent;
        [SerializeField] protected PlacementInteractionEvent dragEndSocketSuccessEvent;
        [SerializeField] protected PlacementInteractionEvent dragEndSocketFailedEvent;
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
                    _activeComponent = poc;
                    _activePlacement = poc.PlacementData;
                    _startPos = poc.RootPlacement.position;
                    _startRot = poc.RootPlacement.rotation;
                    _dragTarget = poc.RootPlacement;
                    
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
        #region Drag Additional
        void UpdateFreeDrag(Ray ray)
        {
            if (_dragTarget == null) return;

            Vector3 worldPoint = ray.GetPoint(_dragRayDistance);
            _dragTarget.position = worldPoint + _dragLocalOffset;
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
            if(_previousHoverSocket!= newHover)
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

            // OLD
            var hits = Physics.RaycastAll(ray, maxRayDistance, placementMask);
            if (drawDebug)
            {
                Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, Color.orange, 0.5f);
            }
            FP_PlacementSocketComponent foundSocket = null;

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent(out FP_PlacementSocketComponent socket))
                {
                    if (drawDebug)
                    {
                        Debug.Log($"Hit something with a FP_PlacementSocketComponent: {socket.name}");
                    }
                    if (!socket.CanAccept(_activePlacement))
                        continue;

                    if (socket.TryGetPreviewPose(_activePlacement, hit, out var pose))
                    {
                        // Apply preview directly
                        _activeComponent.RootPlacement.SetPositionAndRotation(
                            pose.position,
                            pose.rotation
                        );
                        foundSocket = socket;
                        if (drawDebug)
                        {
                            Debug.Log($"Found Socket: {socket.name}");
                        }
                        break;
                    }
                }
            }
            // If no valid socket, do nothing (or optionally snap back later)
            _activeSocket = foundSocket;
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
                dragEndSocketSuccessEvent?.Invoke(_activePlacement, _activeSocket);
            }
            else
            {
                _activeComponent.transform.SetPositionAndRotation(_startPos, _startRot);
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
