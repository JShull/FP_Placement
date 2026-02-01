namespace FuzzPhyte.Placement.Interaction
{
    using UnityEngine; 

    public class FP_PlacementInteractionBehaviour : PlacementBaseInput
    {
        [Header("Placement")]
        [SerializeField] private float maxRayDistance = 100f;

        private PlacementObject _activePlacement;
        private PlacementObjectComponent _activeComponent;
        private FP_PlacementSocketComponent _activeSocket;

        [SerializeField] private Vector3 _startPos;
        [SerializeField] private Quaternion _startRot;

        protected override void UpdateLogic()
        {
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
        protected void TryBegin(Ray ray)
        {
            if (Physics.Raycast(ray, out var hit, maxRayDistance))
            {
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

            FP_PlacementSocketComponent foundSocket = null;

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent(out FP_PlacementSocketComponent socket))
                {
                    if (!socket.CanAccept(_activePlacement))
                        continue;

                    if (socket.TryGetPreviewPose(_activePlacement, hit, out var pose))
                    {
                        // Apply preview directly
                        _activeComponent.transform.SetPositionAndRotation(
                            pose.position,
                            pose.rotation
                        );
                        foundSocket = socket;
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
            }
            else
            {
                _activeComponent.transform.SetPositionAndRotation(_startPos, _startRot);
            }

            _activePlacement = null;
            _activeComponent = null;
            _activeSocket = null;
        }
    }
}
