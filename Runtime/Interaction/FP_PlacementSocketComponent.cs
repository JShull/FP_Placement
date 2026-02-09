namespace FuzzPhyte.Placement.Interaction
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    [System.Serializable]
    public class PlacementSocketHoverEvent : UnityEvent<FP_PlacementSocketComponent> { }
    public class PlacementObjectEvent:UnityEvent<PlacementObjectComponent> { }
    public class FP_PlacementSocketComponent : MonoBehaviour
    {
        [SerializeField] Transform SocketLocation;
        [Header("Socket Rules")]
        [Tooltip("Use Ignore for a return true statement, use layout for false")]
        [SerializeField] private PlacementBuildMode _buildMode = PlacementBuildMode.Stacking;

        [Tooltip("Optional category filter. If empty, all are allowed.")]
        [SerializeField] private List<PlacementCategory> _allowedCategories = new();

        [Header("Stacking / Volume Settings")]
        [SerializeField] private Vector3 _localStackAxis = Vector3.right;
        [SerializeField] private float _capacity = 1.0f;
        public float Capacity
        {
            get => _capacity;
            set => _capacity = Mathf.Max(0f, value);
        }
        [Space]
        private readonly HashSet<PlacementObjectComponent> _occupants = new();

        [Header("Hover Events")]
        [SerializeField] private PlacementSocketHoverEvent onHoverEnter;
        [SerializeField] private PlacementSocketHoverEvent onHoverExit;
        [SerializeField] private PlacementSocketHoverEvent onDragEnd;
        //public PlacementObjectEvent AddPlacementEvent;
        //public PlacementObjectEvent RemovePlacementEvent;
        public System.Action<PlacementObjectComponent>OnPlacementAddedAction;
        public System.Action<PlacementObjectComponent>OnPlacementRemovedAction;

        private bool _isHovered;
        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        private float _usedCapacity = 0f;
        protected void Awake()
        {
            if (SocketLocation == null)
            {
                SocketLocation = this.transform;
            }
        }
        #region Public API

        public bool CanAccept(PlacementObjectComponent placement)
        {
            if (placement == null)
                return false;

            // Already registered here → allow hover but not re-add
            if (_occupants.Contains(placement))
                return false;

            if (placement.PlacementData.BuildMode != _buildMode)
                return false;

            if (!CategoryAllowed(placement.PlacementData))
                return false;

            switch (_buildMode)
            {
                case PlacementBuildMode.Ignore:
                    return true;
                case PlacementBuildMode.Stacking:
                    return CanAcceptStacking(placement.PlacementData);
                case PlacementBuildMode.Layout:
                    return false;
            }

            return false;
        }

        public bool TryGetPreviewPose(PlacementObjectComponent placement, RaycastHit hit, out Pose pose)
        {
            pose = default;

            if (!CanAccept(placement))
                return false;

            switch (_buildMode)
            {
                case PlacementBuildMode.Stacking:
                    return TryGetStackingPose(placement, out pose);
                case PlacementBuildMode.Ignore:
                    return TryGetIgnorePose(placement, out pose);
            }

            return false;
        }
        public void CommitPlacement(PlacementObjectComponent placement, Transform instance)
        {
            if (placement == null || instance == null)
                return;

            if (!CanAccept(placement))
                return;

            switch (_buildMode)
            {
                case PlacementBuildMode.Stacking:
                    CommitStacking(placement, instance);
                    break;
                case PlacementBuildMode.Ignore:
                    CommitIgnore(placement, instance);
                    break;
            }
        }
        public void SetHoverState(bool hovered)
        {
            if (_isHovered == hovered)
                return;

            _isHovered = hovered;

            if (_isHovered)
                onHoverEnter?.Invoke(this);
            else
                onHoverExit?.Invoke(this);
        }

        #endregion
        private bool CategoryAllowed(PlacementObject placement)
        {
            if (_allowedCategories == null || _allowedCategories.Count == 0)
                return true;

            foreach (var cat in placement.Categories)
            {
                if (_allowedCategories.Contains(cat))
                    return true;
            }

            return false;
        }
        private bool CanAcceptStacking(PlacementObject placement)
        {
            float width = Mathf.Max(0f, placement.StackSize.x);
            return (_usedCapacity + width) <= _capacity;
        }
        private bool TryGetStackingPose(PlacementObjectComponent placement, out Pose pose)
        {
            float width = placement.PlacementData.StackSize.x;
            float half = width * 0.5f;

            float offset = _usedCapacity + half;

            Vector3 localPos = _localStackAxis.normalized * offset;
            Vector3 worldPos = transform.TransformPoint(localPos);

            Quaternion worldRot = transform.rotation;

            pose = new Pose(worldPos, worldRot);
            return true;
        }
        private bool TryGetIgnorePose(PlacementObjectComponent placement, out Pose pose)
        {
            if (SocketLocation != null)
            {
                pose = new Pose(SocketLocation.position, SocketLocation.rotation);
                return true;
            }
            else
            {
                pose = new Pose(transform.position, transform.rotation);
            }
            return false;
        }
        private void CommitStacking(PlacementObjectComponent placement, Transform instance)
        {
            if (!TryGetStackingPose(placement, out var pose))
                return;

            instance.SetPositionAndRotation(pose.position, pose.rotation);
            RegisterPlacement(placement,instance);
        }
        private void CommitIgnore(PlacementObjectComponent placement, Transform instance)
        {
            if (SocketLocation != null)
                instance.SetPositionAndRotation(SocketLocation.position, SocketLocation.rotation);

            RegisterPlacement(placement,instance);
            onDragEnd?.Invoke(this);
        }

        #region Accounting for Occupants
        private float GetPlacementSize(PlacementObjectComponent placement, Transform instance)
        {
            if (placement == null)
                return 0f;

            return Mathf.Max(0f, placement.PlacementData.StackSize.x);
        }
        private void RegisterPlacement(PlacementObjectComponent placement, Transform instance)
        {
            if (_occupants.Add(placement))
            {
                _usedCapacity += GetPlacementSize(placement,instance);
                Debug.Log($"Registered placement: {placement.name}, used capacity now {_usedCapacity}/{_capacity}");
                if(instance != null)
                {
                    var PC= instance.gameObject.GetComponent<PlacementObjectComponent>();
                    if (PC!=null)
                    {
                        //AddPlacementEvent?.Invoke(PC);
                        OnPlacementAddedAction?.Invoke(PC);
                        Debug.Log($"Invoked AddPlacementEvent for {placement.name}");
                    }
                }
                
            }
        }
        public void RemovePlacement(PlacementObjectComponent placement,Transform instance)
        {
            if (placement == null)
                return;

            if (_occupants.Remove(placement))
            {
                _usedCapacity -= GetPlacementSize(placement,instance);
                _usedCapacity = Mathf.Max(0f, _usedCapacity);
                if(instance != null)
                {
                    var PC= instance.gameObject.GetComponent<PlacementObjectComponent>();
                    if (PC!=null)
                    {
                        //RemovePlacementEvent?.Invoke(PC);
                        OnPlacementRemovedAction?.Invoke(PC);
                    }
                }  
            }
        }

        #endregion
        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos) return;

            Gizmos.color = Color.cyan;

            Vector3 start = transform.position;
            Vector3 end = transform.TransformPoint(_localStackAxis.normalized * _capacity);

            Gizmos.DrawLine(start, end);

            Gizmos.color = Color.yellow;
            Vector3 usedEnd = transform.TransformPoint(_localStackAxis.normalized * _usedCapacity);
            Gizmos.DrawLine(start, usedEnd);
        }

    }
}
