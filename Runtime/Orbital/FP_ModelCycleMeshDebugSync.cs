namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using FuzzPhyte.Utility;

    [DisallowMultipleComponent]
    public sealed class FP_ModelCycleMeshDebugSync : MonoBehaviour
    {
        [SerializeField] private FP_ModelCycleController _cycle;
        [SerializeField] private FPRuntimeMeshViewer _meshViewer;

        [Header("Renderer Collection")]
        [SerializeField] private bool _includeInactiveChildren = false;
        [SerializeField] private bool _includeSkinnedMeshRenderers = true;
        [SerializeField] private bool _includeMeshRenderers = true;

        private void Reset()
        {
            _cycle = GetComponent<FP_ModelCycleController>();
            _meshViewer = FindFirstObjectByType<FPRuntimeMeshViewer>();
        }

        private void OnEnable()
        {
            if (_cycle == null || _meshViewer == null) return;

            _cycle.OnActiveModelChanged += HandleActiveModelChanged;

            // Force-sync once at enable
            HandleActiveModelChanged(_cycle.ActiveIndex, _cycle.ActiveModel);
        }

        private void OnDisable()
        {
            if (_cycle != null)
                _cycle.OnActiveModelChanged -= HandleActiveModelChanged;
        }

        private void HandleActiveModelChanged(int index, FP_ModelDisplayBinding active)
        {
            if (_meshViewer == null)
                return;

            if (active == null)
            {
                _meshViewer.SetTargets(System.Array.Empty<Renderer>());
                return;
            }

            // Collect renderers from ONLY the active model root
            // (This matches your "show one model at a time" logic.) :contentReference[oaicite:2]{index=2}
            var renderers = active.GetComponentsInChildren<Renderer>(_includeInactiveChildren);

            // Optional filtering by type
            if (!_includeSkinnedMeshRenderers || !_includeMeshRenderers)
            {
                var tmp = new System.Collections.Generic.List<Renderer>(renderers.Length);
                foreach (var r in renderers)
                {
                    if (r == null) continue;

                    if (!_includeSkinnedMeshRenderers && r is SkinnedMeshRenderer) continue;
                    if (!_includeMeshRenderers && r is MeshRenderer) continue;

                    tmp.Add(r);
                }
                renderers = tmp.ToArray();
            }

            _meshViewer.SetTargets(renderers);
        }
    }
}
