namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;

    [DisallowMultipleComponent]
    public sealed class FP_ModelDisplayBinding : MonoBehaviour
    {
        [SerializeField] private FP_ModelDisplayData _data;

        public FP_ModelDisplayData Data => _data;

        /// <summary>
        /// Returns world-space bounds that should be used by the orbital camera system.
        /// </summary>
        public Bounds GetWorldBounds()
        {
            if (_data != null && _data.UseLocalBoundsOverride)
            {
                // Local override converted to world using root transform
                var b = new Bounds(_data.LocalBoundsCenter, _data.LocalBoundsSize);
                return TransformLocalBoundsToWorld(b, transform);
            }

            // Fallback: compute renderer bounds
            var renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers == null || renderers.Length == 0)
            {
                // No renderers: return a small bounds at root
                return new Bounds(transform.position, Vector3.one * 0.25f);
            }

            Bounds wb = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                wb.Encapsulate(renderers[i].bounds);

            return wb;
        }

        public void ApplyPresentationDefaults(Transform displayPivot)
        {
            if (_data == null) return;

            // Place at pivot
            if (displayPivot != null)
            {
                transform.position = displayPivot.position;
                transform.rotation = displayPivot.rotation;

                // Apply local pivot offset
                transform.position += transform.TransformVector(_data.LocalPivotOffset);
            }

            // Apply default local rotation/scale
            transform.localRotation = Quaternion.Euler(_data.DefaultLocalEuler);
            transform.localScale = Vector3.one * Mathf.Max(0.0001f, _data.DefaultUniformScale);
        }

        private static Bounds TransformLocalBoundsToWorld(Bounds localBounds, Transform root)
        {
            // Conservative: transform the 8 corners and encapsulate
            Vector3 c = localBounds.center;
            Vector3 e = localBounds.extents;

            Vector3[] corners =
            {
                c + new Vector3( e.x,  e.y,  e.z),
                c + new Vector3( e.x,  e.y, -e.z),
                c + new Vector3( e.x, -e.y,  e.z),
                c + new Vector3( e.x, -e.y, -e.z),
                c + new Vector3(-e.x,  e.y,  e.z),
                c + new Vector3(-e.x,  e.y, -e.z),
                c + new Vector3(-e.x, -e.y,  e.z),
                c + new Vector3(-e.x, -e.y, -e.z),
            };

            Bounds world = new Bounds(root.TransformPoint(corners[0]), Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                world.Encapsulate(root.TransformPoint(corners[i]));

            return world;
        }
    }
}
