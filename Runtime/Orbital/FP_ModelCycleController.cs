namespace FuzzPhyte.Placement.OrbitalCamera
{
    using FuzzPhyte.Utility;
    using System;
    using System.Collections;
    using UnityEngine;
    [DisallowMultipleComponent]
    public sealed class FP_ModelCycleController : MonoBehaviour
    {
        [Header("Model Set (bindings)")]
        [SerializeField] private FP_ModelDisplayBinding[] _models;

        [Header("Placement")]
        [Tooltip("Where the active model should be positioned (center of bounds).")]
        [SerializeField] private Transform _displayPivot;

        [Tooltip("If true, moves active model to pivot position.")]
        [SerializeField] private bool _snapToPivot = true;

        [Tooltip("If true, resets local rotation of active model at pivot.")]
        [SerializeField] private bool _resetRotationOnShow = true;

        [Header("State")]
        [SerializeField] private int _activeIndex;
        //[SerializeField] private FP_ToolbarAction _activeVisualAction;
        [SerializeField] private FPMeshViewStatus _activeMeshViewStatus;
        [Tooltip("If you want to scale your grid based on the model...")]
        [SerializeField] private FPRuntimeGridPlane _gridPlaneXZ;
        [SerializeField] private FPRuntimeGridPlane _gridPlaneXY;
        [SerializeField] private FP_MeasurementToolController _measurementController;
        [SerializeField] private FPRuntimeMeshViewer _meshViewer;
        public FPMeshViewStatus ActiveMeshViewStatus => _activeMeshViewStatus;
        public int ActiveIndex => _activeIndex;
        //public FP_ToolbarAction ActiveVisualAction => _activeVisualAction;
        public int Count => _models?.Length ?? 0;
        public FP_ModelDisplayBinding ActiveModel => (Count > 0 && _activeIndex >= 0 && _activeIndex < Count) ? _models[_activeIndex] : null;

        public event Action<int, FP_ModelDisplayBinding> OnActiveModelChanged;
        public event Action<int, FP_ModelDisplayBinding, FPMeshViewStatus> OnActiveVisualActionChanged;

        private void Awake()
        {
            StartCoroutine(DelayAwakeReset());
        }

        IEnumerator DelayAwakeReset()
        {
            yield return new WaitForEndOfFrame();
            ApplyActiveModel(force: true);
            _activeMeshViewStatus = new FPMeshViewStatus()
            {
                Flags = FPMeshViewFlags.Renderer,
                SurfaceMode = MeshSurfaceDebugMode.None,
                ShowRenderer = true
            };
        }

        public void SetModels(FP_ModelDisplayBinding[] models, int startIndex = 0)
        {
            _models = models;
            _activeIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, Count - 1));
            ApplyActiveModel(force: true);
        }

        public void Next()
        {
            if (Count == 0) return;
            _activeIndex = (_activeIndex + 1) % Count;
            ApplyActiveModel(force: false);
        }

        public void Prev()
        {
            if (Count == 0) return;
            _activeIndex = (_activeIndex - 1 + Count) % Count;
            ApplyActiveModel(force: false);
        }
        #region Grid Related public Functions
        public void TurnOnGridXZ()
        {
            if (_gridPlaneXZ != null)
            {
                _gridPlaneXZ.gameObject.SetActive(true);
            }
        }
        public void TurnOffGridXZ()
        {
            if (_gridPlaneXZ != null)
            {
                _gridPlaneXZ.gameObject.SetActive(false);
            }
        }
        public void TurnOnGridXY()
        {
            if (_gridPlaneXY != null)
            {
                _gridPlaneXY.gameObject.SetActive(true);
            }
        }
        public void TurnOffGridXY()
        {
            if (_gridPlaneXY != null)
            {
                _gridPlaneXY.gameObject.SetActive(false);
            }
        }
        #endregion
        public void SetVisualInformation(FP_ToolbarAction action)
        {
            switch (action)
            {
                case FP_ToolbarAction.ToggleVerticesOn:
                    _activeMeshViewStatus.Flags |= FPMeshViewFlags.Vertices;
                    break;
                case FP_ToolbarAction.ToggleVerticesOff:
                    _activeMeshViewStatus.Flags &= ~FPMeshViewFlags.Vertices;
                    break;
                case FP_ToolbarAction.ToggleWireframeOn:
                    _activeMeshViewStatus.Flags |= FPMeshViewFlags.Wireframe;
                    break;
                case FP_ToolbarAction.ToggleWireframeOff:
                    _activeMeshViewStatus.Flags &= ~FPMeshViewFlags.Wireframe;
                    break;
                case FP_ToolbarAction.ToggleBoundsOn:
                    _activeMeshViewStatus.Flags |= FPMeshViewFlags.Bounds;
                    break;
                case FP_ToolbarAction.ToggleBoundsOff:
                    _activeMeshViewStatus.Flags &= ~FPMeshViewFlags.Bounds;
                    break;
                case FP_ToolbarAction.ToggleRendererOn:
                    _activeMeshViewStatus.Flags |= FPMeshViewFlags.Renderer;
                    _activeMeshViewStatus.ShowRenderer = true;
                    break;
                case FP_ToolbarAction.ToggleRendererOff:
                    _activeMeshViewStatus.Flags &= ~FPMeshViewFlags.Renderer;
                    _activeMeshViewStatus.ShowRenderer = false;
                    break;
            }
            OnActiveVisualActionChanged?.Invoke(_activeIndex, ActiveModel, _activeMeshViewStatus);
        }
        public void SetIndex(int index)
        {
            if (Count == 0) return;
            int clamped = Mathf.Clamp(index, 0, Count - 1);
            if (clamped == _activeIndex) return;

            _activeIndex = clamped;
            ApplyActiveModel(force: false);
        }

        public void HideAll()
        {
            if (_models == null) return;
            for (int i = 0; i < _models.Length; i++)
            {
                if (_models[i] != null) _models[i].gameObject.SetActive(false);
            }
        }

        private void ApplyActiveModel(bool force)
        {
            if (_models == null || _models.Length == 0)
            {
                OnActiveModelChanged?.Invoke(-1, null);
                return;
            }

            // Hide all except active
            for (int i = 0; i < _models.Length; i++)
            {
                var go = _models[i];
                if (go == null) continue;

                bool shouldShow = (i == _activeIndex);
                if (force || go.gameObject.activeSelf != shouldShow)
                {
                    go.gameObject.SetActive(shouldShow);
                }  
            }

            var active = _models[_activeIndex];
            if (active != null && _displayPivot != null)
            {
                if (_snapToPivot)
                    active.transform.position = _displayPivot.position;

                if (_resetRotationOnShow)
                    active.transform.rotation = Quaternion.identity;

                // set/update grid if we have one
                var measureDetails = active.gameObject.GetComponent<FP_MeasurementHitProvider>();
                
                if (_gridPlaneXZ != null&& measureDetails!=null)
                {
                    UpdateGridPattern(_gridPlaneXZ, measureDetails);
                }
                if(_gridPlaneXY!=null && measureDetails!=null)
                {
                    UpdateGridPattern(_gridPlaneXY, measureDetails);
                }
                
                // get label UI ref
                var measurementLabelUI = _measurementController.gameObject.GetComponent<FP_MeasurementLabelUI>();
                //set/update measurement tool if we have one
                if (_measurementController != null && measureDetails!=null&& measurementLabelUI!=null)
                {
                    var measureOverlayDetails = _measurementController.Overlay;
                    if (measureOverlayDetails != null)
                    {
                        switch (measureDetails.ModelUnits)
                        {
                            case UnitOfMeasure.Inch:
                                measureOverlayDetails.LineWidthWorld = 0.0025f;
                                measureOverlayDetails.PointSize = 0.005f;
                                measurementLabelUI.UpdateOffsetDetails(0.0254f, 0.0254f, UnitOfMeasure.Centimeter);
                                if (_meshViewer != null)
                                {
                                    _meshViewer.UpdateVertexSizing(0.001f);
                                }
                                break;
                            case UnitOfMeasure.Meter:
                                measureOverlayDetails.LineWidthWorld = 0.01f;
                                measureOverlayDetails.PointSize = 0.05f;
                                measurementLabelUI.UpdateOffsetDetails(0.3f,0.2f, UnitOfMeasure.Meter);
                                if (_meshViewer != null)
                                {
                                    _meshViewer.UpdateVertexSizing(0.015f);
                                }
                                break;
                            case UnitOfMeasure.Centimeter:
                                measureOverlayDetails.LineWidthWorld = 0.001f;
                                measureOverlayDetails.PointSize = 0.005f;
                                measurementLabelUI.UpdateOffsetDetails(0.03f, 0.02f, UnitOfMeasure.Centimeter);
                                if (_meshViewer != null)
                                {
                                    _meshViewer.UpdateVertexSizing(0.0025f);
                                }
                                break;
                            case UnitOfMeasure.Feet:
                                measureOverlayDetails.LineWidthWorld = 0.025f;
                                measureOverlayDetails.PointSize = 0.05f;
                                measurementLabelUI.UpdateOffsetDetails(0.1f, 0.066f, UnitOfMeasure.Centimeter);
                                if (_meshViewer != null)
                                {
                                    _meshViewer.UpdateVertexSizing(0.0083f);
                                }
                                break;
                        }
                        measureOverlayDetails.ClearMeasurement();
                    }
                }
            }


            OnActiveModelChanged?.Invoke(_activeIndex, active);
        }
        private void UpdateGridPattern(FPRuntimeGridPlane gridPlane,FP_MeasurementHitProvider hitProvider)
        {
            if (gridPlane != null)
            {
                var units = hitProvider.ModelUnits;
                switch (units)
                {
                    case UnitOfMeasure.Inch:
                        gridPlane.Units = UnitOfMeasure.Inch;
                        gridPlane.MajorSpacingInUnits = 12;
                       
                        break;
                    case UnitOfMeasure.Meter:
                        gridPlane.Units = UnitOfMeasure.Meter;
                        gridPlane.MajorSpacingInUnits = 1;
                        break;
                    case UnitOfMeasure.Centimeter:
                        gridPlane.Units = UnitOfMeasure.Centimeter;
                        gridPlane.MajorSpacingInUnits = 100;
                        break;
                    case UnitOfMeasure.Feet:
                        gridPlane.Units = UnitOfMeasure.Feet;
                        gridPlane.MajorSpacingInUnits = 10;
                        break;
                }
            }
            gridPlane.RecalculateWorldSpacing();
            if (hitProvider.GridWorldPivot != null)
            {
                gridPlane.transform.position = hitProvider.GridWorldPivot.position;
            }
           
           
           
        }
    }
}
