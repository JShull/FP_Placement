namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using FuzzPhyte.Utility;

    // Syncs the active model from FP_ModelCycleController to a FPRuntimeMeshViewer for debugging.
    // Also sets our mesh visuals for Runtime Mesh Viewer Options based on toolbar actions and starting conditions

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
            _cycle.OnActiveVisualActionChanged += HandleMeshVisualChanges;

            // Initial sync
            HandleActiveModelChanged(_cycle.ActiveIndex, _cycle.ActiveModel);
            HandleMeshVisualChanges(_cycle.ActiveIndex, _cycle.ActiveModel, _cycle.ActiveVisualAction);
        }

        private void OnDisable()
        {
            if (_cycle == null) return;
            
            _cycle.OnActiveModelChanged -= HandleActiveModelChanged;
            _cycle.OnActiveVisualActionChanged -= HandleMeshVisualChanges;
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
           
            var renderers = GetRenderersFromActiveModel(active);
            _meshViewer.SetTargets(renderers);
        }
        private Renderer[] GetRenderersFromActiveModel(FP_ModelDisplayBinding active)
        {
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
            return renderers;
        }
        private void HandleMeshVisualChanges(int index, FP_ModelDisplayBinding active, FP_ToolbarAction action)
        {
            if (_meshViewer == null)
                return;

            if (active == null)
            {
                _meshViewer.SetTargets(System.Array.Empty<Renderer>());
                return;
            }
            switch (action)
            {
                case FP_ToolbarAction.ToggleVerticesOn:
                    _meshViewer.SetMeshModeType(MeshViewMode.Vertices, GetRenderersFromActiveModel(active));
                    break;
                case FP_ToolbarAction.ToggleWireframeOn:
                    _meshViewer.SetMeshModeType(MeshViewMode.Wireframe, GetRenderersFromActiveModel(active));
                    break;
                case FP_ToolbarAction.ToggleVerticesOff:
                case FP_ToolbarAction.ToggleWireframeOff:
                    _meshViewer.SetMeshModeType(MeshViewMode.Default);
                    break;
                // Extend with more cases as needed for wireframe, bounds, etc.
                default:
                    break;
            }
        }
    }
}
