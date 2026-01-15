namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    public class FP_ToolbarBinder : MonoBehaviour
    {
        [SerializeField] private FP_ToolbarUIRaycaster _raycaster;
        [SerializeField] private FP_ModelCycleController _modelCycle;
        
        // Optional: hooks to other systems (vertices, wireframe, etc.)
        //[SerializeField] private MonoBehaviour _verticesSystem; // replace with your concrete type

        private void OnEnable()
        {
            if (_raycaster != null)
                _raycaster.OnToolbarAction += HandleToolbarAction;
        }

        private void OnDisable()
        {
            if (_raycaster != null)
                _raycaster.OnToolbarAction -= HandleToolbarAction;
        }

        private void HandleToolbarAction(FP_ToolbarAction action, FP_ToolbarHitProvider provider, RaycastHit hit)
        {
            switch (action)
            {
                case FP_ToolbarAction.NextModel:
                    _modelCycle?.Next();
                    break;

                case FP_ToolbarAction.PrevModel:
                    _modelCycle?.Prev();
                    break;

                case FP_ToolbarAction.ToggleVerticesOn:
                    if (_modelCycle != null)
                    {
                        // keep the same model index but change visual info
                        _modelCycle.SetIndex(_modelCycle.ActiveIndex);
                        _modelCycle.SetVisualInformation(FP_ToolbarAction.ToggleVerticesOn);
                    }

                    break;
                case FP_ToolbarAction.ToggleVerticesOff:
                if(_modelCycle != null)
                    {
                        // keep the same model index but change visual info
                        _modelCycle.SetIndex(_modelCycle.ActiveIndex);
                        _modelCycle.SetVisualInformation(FP_ToolbarAction.ToggleVerticesOff);
                    }  
                    break;

                // Extend:
                // ToggleWireframe, ToggleBounds, ResetModelPose, etc.

                default:
                    break;
            }
        }
    }
}
