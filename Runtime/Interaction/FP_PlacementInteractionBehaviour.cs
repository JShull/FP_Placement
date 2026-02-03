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
                TryBegin(ray);
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
        protected void TryBegin(Ray ray)
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
                    _startPos = poc.transform.position;
                    _startRot = poc.transform.rotation;
                }
            }
        }
        protected void UpdateDrag(Ray ray)
        {
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
            
            _activePlacement = null;
            _activeComponent = null;
            _activeSocket = null;
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
