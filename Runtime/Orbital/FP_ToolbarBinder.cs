namespace FuzzPhyte.Placement.OrbitalCamera
{
    using System;
    using UnityEngine;
    public class FP_ToolbarBinder : MonoBehaviour
    {
        [SerializeField] private FP_ToolbarUIRaycaster _raycaster;
        [SerializeField] private FP_ModelCycleController _modelCycle;
        [SerializeField] private FP_OrbitalMouseInputBehaviour _orbitalMouseInputBehaviour;
        
        //Other UI controllable items - maybe move these out
        public event Action OnMeasureToolActivated;
        public event Action OnMeasureToolDeactivated;
        public event Action OnMeasureToolReset;
        public event Action OnMeasureAngleIncrementActivated;
        public event Action OnMeasureAngleIncrementDeactivated;

        //orbit & Pan
        public event Action OnOrbitModeActivated;
        public event Action OnPanModeActivated;

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
        /// <summary>
        /// Call if we need to manually handle a toolbar action outside of the raycaster
        /// </summary>
        /// <param name="action"></param>
        public void ReceiveProviderOnStart(FP_ToolbarHitProvider provider)
        {
            if (provider == null) return;
            // If you don't care about hit on Start, just pass a default hit:
            HandleToolbarAction(provider.Action, provider, default);
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
                        _modelCycle.SetIndex(_modelCycle.ActiveIndex);
                        _modelCycle.SetVisualInformation(FP_ToolbarAction.ToggleVerticesOn);
                    }

                    break;
                case FP_ToolbarAction.ToggleVerticesOff:
                    if(_modelCycle != null)
                        {
                            _modelCycle.SetIndex(_modelCycle.ActiveIndex);
                            _modelCycle.SetVisualInformation(FP_ToolbarAction.ToggleVerticesOff);
                        }  
                    break;
                case FP_ToolbarAction.ToggleBoundsOn:
                    if (_modelCycle != null)
                    {
                        _modelCycle.SetIndex(_modelCycle.ActiveIndex);
                        _modelCycle.SetVisualInformation(FP_ToolbarAction.ToggleBoundsOn);
                    }
                    break;
                case FP_ToolbarAction.ToggleBoundsOff:
                    if (_modelCycle != null)
                    {
                        _modelCycle.SetIndex(_modelCycle.ActiveIndex);
                        _modelCycle.SetVisualInformation(FP_ToolbarAction.ToggleBoundsOff);
                    }
                    break;
                case FP_ToolbarAction.ToggleWireframeOn:
                    if (_modelCycle != null)
                    {
                        _modelCycle.SetIndex(_modelCycle.ActiveIndex);
                        _modelCycle.SetVisualInformation(FP_ToolbarAction.ToggleWireframeOn);
                    }
                    break;
                case FP_ToolbarAction.ToggleWireframeOff:
                    if (_modelCycle != null)
                    {
                        _modelCycle.SetIndex(_modelCycle.ActiveIndex);
                        _modelCycle.SetVisualInformation(FP_ToolbarAction.ToggleWireframeOff);
                    }
                    break;
                case FP_ToolbarAction.ToggleRendererOn:
                    if (_modelCycle != null)
                    {
                        _modelCycle.SetIndex(_modelCycle.ActiveIndex);
                        _modelCycle.SetVisualInformation(FP_ToolbarAction.ToggleRendererOn);
                    }
                    break;
                case FP_ToolbarAction.ToggleRendererOff:
                    if (_modelCycle != null)
                    {
                        _modelCycle.SetIndex(_modelCycle.ActiveIndex);
                        _modelCycle.SetVisualInformation(FP_ToolbarAction.ToggleRendererOff);
                    }
                    break;
                case FP_ToolbarAction.ToggleMeasurementOn:
                    OnMeasureToolActivated?.Invoke();
                    break;
                case FP_ToolbarAction.ToggleMeasurementOff:
                    OnMeasureToolDeactivated?.Invoke();
                    break;
                case FP_ToolbarAction.ToolMeasureReset:
                    OnMeasureToolReset?.Invoke();
                    break;
                case FP_ToolbarAction.OrbitMode:
                    if (_orbitalMouseInputBehaviour != null)
                    {
                        _orbitalMouseInputBehaviour.SetMode(FP_OrbitalMouseMode.Orbit);
                    }
                    OnOrbitModeActivated?.Invoke();
                    break;
                case FP_ToolbarAction.PanMode:
                    if (_orbitalMouseInputBehaviour != null)
                    {
                        _orbitalMouseInputBehaviour.SetMode(FP_OrbitalMouseMode.Pan);
                    }
                    OnPanModeActivated?.Invoke();
                    break;
                case FP_ToolbarAction.ResetModelPose:
                    if (_orbitalMouseInputBehaviour != null)
                    {
                        _orbitalMouseInputBehaviour.RecenterBounds();
                    }
                    break;
                case FP_ToolbarAction.GridXZOn:
                    if (_modelCycle != null) _modelCycle.TurnOnGridXZ();
                    break;
                case FP_ToolbarAction.GridXZOff:
                    if (_modelCycle != null) _modelCycle.TurnOffGridXZ();
                    break;
                case FP_ToolbarAction.GridXYOn:
                    if (_modelCycle != null) _modelCycle.TurnOnGridXY();
                    break;
                case FP_ToolbarAction.GridXYOff:
                    if (_modelCycle != null) _modelCycle.TurnOffGridXY();
                    break;
                case FP_ToolbarAction.ToolMeasureAngleOn:
                    OnMeasureAngleIncrementActivated?.Invoke();
                    break;
                case FP_ToolbarAction.ToolMeasureAngleOff:
                    OnMeasureAngleIncrementDeactivated?.Invoke();
                    break;
            }
        }
    }
}
