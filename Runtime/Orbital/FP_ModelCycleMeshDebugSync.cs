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
            HandleMeshVisualChanges(_cycle.ActiveIndex, _cycle.ActiveModel, _cycle.ActiveMeshViewStatus);
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
           
            var renderers = GetRenderersFromActiveModel(active,_cycle.ActiveMeshViewStatus.ShowRenderer);
            _meshViewer.SetTargets(renderers,_cycle.ActiveMeshViewStatus.ShowRenderer);
        }
        
        private Renderer[] GetRenderersFromActiveModel(FP_ModelDisplayBinding active,bool renderMainItem=true)
        {
            //var renderers = active.GetComponentsInChildren<Renderer>(_includeInactiveChildren);
            var renderers = active.AllRenderers.ToArray();
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
            // turn off/on main item renderer
            active.SetRendererOnOff(renderMainItem);
            return renderers;
        }
        
        private void HandleMeshVisualChanges(int index, FP_ModelDisplayBinding active, FPMeshViewStatus action)
        {
            if (_meshViewer == null)
                return;

            if (active == null)
            {
                _meshViewer.SetTargets(System.Array.Empty<Renderer>());
                return;
            }
            MeshViewMode mode = ToMeshViewMode(action);
            _meshViewer.SetMeshModeType(mode, GetRenderersFromActiveModel(active, _cycle.ActiveMeshViewStatus.ShowRenderer));
        }

        #region Mesh View Status Extension Work
        public static MeshViewMode ToMeshViewMode(in FPMeshViewStatus status)
        {
            // 1) Surface debug overrides (these are mutually exclusive by design)
            switch (status.SurfaceMode)
            {
                case MeshSurfaceDebugMode.WorldNormals:
                    return MeshViewMode.SurfaceWorldNormals;
                case MeshSurfaceDebugMode.UV0:
                    return MeshViewMode.SurfaceUV0;
                case MeshSurfaceDebugMode.VertexColors:
                    return MeshViewMode.SurfaceVertexColor;
                case MeshSurfaceDebugMode.None:
                default:
                    break;
            }

            // 2) Normals overlay (if you want this to combine with wireframe/vertices later,
            // you can extend MeshViewMode or split into multiple passes)
            if (status.Flags.HasFlag(FPMeshViewFlags.Normals))
                return MeshViewMode.Normals;

            // 3) Topology overlays
            bool showVerts = status.Flags.HasFlag(FPMeshViewFlags.Vertices);
            bool showWire = status.Flags.HasFlag(FPMeshViewFlags.Wireframe);

            if (showVerts && showWire) return MeshViewMode.WireframeAndVertices;
            if (showWire) return MeshViewMode.Wireframe;
            if (showVerts) return MeshViewMode.Vertices;

            // 4) Nothing selected
            return MeshViewMode.Default;
        }

        public static bool ShouldShowRenderer(in FPMeshViewStatus status)
            => status.Flags.HasFlag(FPMeshViewFlags.Renderer);

        public static bool ShouldShowBounds(in FPMeshViewStatus status)
            => status.Flags.HasFlag(FPMeshViewFlags.Bounds);
        #endregion
    }
}
