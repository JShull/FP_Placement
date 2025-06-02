namespace FuzzPhyte.Placement
{
    using UnityEngine;
    using System.Collections.Generic;

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class PlacementVolumeComponent : MonoBehaviour
    {
        [Header("Meta Data")]
        public string VolumeName = "Volume";
        public List<PlacementCategory> AllowedCategories = new();
        public List<PlacementCategory> DisallowedCategories = new();
        public List<PlacementCategory> DiscouragedCategories = new();
        public List<string> ThemeTags = new();
        public float MaxWeight = 100f;
        public int MaxItems = 10;
        public bool StackingAllowed = true;

        [Header("Gizmo Settings")]
        public bool DrawGizmos = true;
        public Color GizmoColor = new Color(0f, 1f, 1f, 0.3f);
        public bool ShowLabel = true;

        private BoxCollider _bounds;

        private void Reset()
        {
            _bounds = GetComponent<BoxCollider>();
            _bounds.isTrigger = true; // Mark as trigger so it doesn't interfere with physics
        }

        private void OnValidate()
        {
            if (_bounds == null) _bounds = GetComponent<BoxCollider>();
        }

        public PlacementVolume GeneratePlacementVolume()
        {
            if (_bounds == null)
            {
                Debug.LogError($"PlacementVolumeComponent '{name}' is missing a BoxCollider.");
                return null;
            }

            var meta = new VolumeMeta
            {
                VolumeName = VolumeName,
                Bounds = _bounds.bounds,
                AllowedCategories = new List<PlacementCategory>(AllowedCategories),
                DisallowedCategories = new List<PlacementCategory>(DisallowedCategories),
                DiscouragedCategories = new List<PlacementCategory>(DiscouragedCategories),
                PriorityWeights = new Dictionary<PlacementCategory, float>(), // Optional future data
                MaxWeight = MaxWeight,
                MaxItems = MaxItems,
                StackingAllowed = StackingAllowed,
                ThemeTags = new List<string>(ThemeTags)
            };

            return new PlacementVolume
            {
                Bounds = _bounds.bounds,
                Meta = meta
            };
        }

        private void OnDrawGizmos()
        {
            if (!DrawGizmos || _bounds == null) return;

            Gizmos.color = GizmoColor;
            Gizmos.DrawWireCube(_bounds.bounds.center, _bounds.bounds.size);
            Gizmos.color = new Color(GizmoColor.r, GizmoColor.g, GizmoColor.b, 0.1f);
            Gizmos.DrawCube(_bounds.bounds.center, _bounds.bounds.size);

#if UNITY_EDITOR
            if (ShowLabel)
            {
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(_bounds.bounds.center + Vector3.up * 0.1f, VolumeName);
            }
#endif
        }

        private void Awake()
        {
            if (_bounds == null) _bounds = GetComponent<BoxCollider>();
        }
    }
}
