namespace FuzzPhyte.Placement.Interaction
{
    using System.Collections.Generic;
    using UnityEngine;
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

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        private float _usedCapacity = 0f;
        protected void Awake()
        {
            if(SocketLocation == null)
            {
                SocketLocation = this.transform;
            }
        }
        #region Public API

        public bool CanAccept(PlacementObject placement)
        {
            if (placement == null)
                return false;

            if (placement.BuildMode != _buildMode)
                return false;

            if (!CategoryAllowed(placement))
                return false;

            switch (_buildMode)
            {
                case PlacementBuildMode.Ignore:
                    return true;
                case PlacementBuildMode.Stacking:
                    return CanAcceptStacking(placement);
                case PlacementBuildMode.Layout:
                    return false;
            }

            return false;
        }

        public bool TryGetPreviewPose(PlacementObject placement,RaycastHit hit,out Pose pose)
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
        public void CommitPlacement(PlacementObject placement,Transform instance)
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
            }
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
        private bool TryGetStackingPose(PlacementObject placement,out Pose pose)
        {
            float width = placement.StackSize.x;
            float half = width * 0.5f;

            float offset = _usedCapacity + half;

            Vector3 localPos = _localStackAxis.normalized * offset;
            Vector3 worldPos = transform.TransformPoint(localPos);

            Quaternion worldRot = transform.rotation;

            pose = new Pose(worldPos, worldRot);
            return true;
        }
        private bool TryGetIgnorePose(PlacementObject placement, out Pose pose)
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
        private void CommitStacking(PlacementObject placement,Transform instance)
        {
            float width = placement.StackSize.x;
            float half = width * 0.5f;

            float offset = _usedCapacity + half;

            Vector3 localPos = _localStackAxis.normalized * offset;
            Vector3 worldPos = transform.TransformPoint(localPos);

            instance.SetPositionAndRotation(worldPos, transform.rotation);

            _usedCapacity += width;
        }
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
