namespace FuzzPhyte.Placement.OrbitalCamera
{
    using System;
    using UnityEngine;
    using FuzzPhyte.Utility;
    using UnityEngine.InputSystem;
    /// <summary>
    /// UI-region gated raycaster for a ViewCube-style 3D overlay.
    ///
    /// Flow:
    /// 1) Reads pointer position from Input System.
    /// 2) Checks if pointer is inside any configured screen region(s).
    /// 3) On click/tap action, raycasts from an orthographic UI camera into the 3D ViewCube colliders.
    /// 4) If hit has a FP_ViewCubeHitProvider, invokes OnHit with that enum.
    ///
    /// Notes:
    /// - custom overlay raycasting
    /// - Your ViewCube colliders should be on a dedicated Layer for clean filtering.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FP_ViewCubeUIRaycaster : FP_UIRegionRaycasterBase<FP_ViewCubeHitProvider>
    {
        public event Action<FP_ViewCubeHitProvider, RaycastHit> OnCubeSelect;
        public event Action<FP_ViewCubeHitProvider> OnViewCubeHover;
        public event Action<FP_ViewCubeHitProvider> OnViewCubeUnHover;

        private void Start()
        {
            if (_lastSelectedProvider != null)
            {
                _lastSelectedProvider.Select();
            }
        }
        protected override string GetDebugTag() => "FP_ViewCubeUIRaycaster";

        protected override void OnSelect(FP_ViewCubeHitProvider provider, RaycastHit hit)
        {
            // selection visuals
            if (_lastSelectedProvider != null && _lastSelectedProvider != provider)
                _lastSelectedProvider.UnSelect();

            provider.Select();
            _lastSelectedProvider = provider;

            OnCubeSelect?.Invoke(provider, hit);
        }

        protected override void OnHover(FP_ViewCubeHitProvider provider, RaycastHit hit)
        {
            provider.Hover();
            OnViewCubeHover?.Invoke(provider);
        }

        protected override void OnUnHover(FP_ViewCubeHitProvider provider)
        {
            provider.UnHover();
            OnViewCubeUnHover?.Invoke(provider);
        }

        protected override void OnUnSelect(FP_ViewCubeHitProvider provider)
        {
            provider.UnSelect();
        }
        /*
        public event Action<FP_ViewCubeHitProvider,RaycastHit> OnCubeSelect;
        public event Action<FP_ViewCubeHitProvider> OnViewCubeHover;
        public event Action<FP_ViewCubeHitProvider> OnViewCubeUnHover;

        [Header("UI Ortho Camera")]
        [SerializeField] private Camera _uiOrthoCamera;

        [Header("Regions (screen space)")]
        [Tooltip("Only when the pointer is within one of these regions will we accept click/tap to raycast.")]

        [SerializeField]
        private FP_ScreenRegionAsset _regions;
        

        [Header("Input Actions (New Input System)")]
        [Tooltip("Value/Vector2 - bind to <Pointer>/position")]
        [SerializeField] private InputActionReference _pointerPosition;

        [Tooltip("Button - bind to <Mouse>/leftButton and/or <Touchscreen>/primaryTouch/press")]
        [SerializeField] private InputActionReference _clickOrTap;

        [Header("Raycast Settings")]
        [Tooltip("LayerMask for your ViewCube colliders.")]
        [SerializeField] private LayerMask _viewCubeLayerMask = ~0;

        [Tooltip("Max ray distance. UI overlay cubes are usually close, but keep it generous.")]
        [SerializeField] private float _maxDistance = 100f;

        [Header("Options")]
        [Tooltip("If true, ignores input when the application isn't focused.")]
        [SerializeField] private bool _requireApplicationFocus = true;

        [Tooltip("If true, click/tap will only raycast when pointer is inside a region.")]
        [SerializeField] private bool _requireRegion = true;

        [Tooltip("If true, logs which region was hit and what collider was hit.")]
        [SerializeField] private bool _debugLogs;

        // cached
        private Vector2 _lastPointerPos;
        private FP_ViewCubeHitProvider _lastProvider;
        [SerializeField]private FP_ViewCubeHitProvider _lastSelected;

        private void Reset()
        {
            //_uiOrthoCamera = Camera.main;
        }

        private void Start()
        {
            if (_lastSelected != null)
            {
                _lastSelected.Select();
            }
        }
        private void OnEnable()
        {
            if (_pointerPosition?.action != null)
            {
                _pointerPosition.action.Enable();
                _pointerPosition.action.performed += OnPointerMove;
                _pointerPosition.action.canceled += OnPointerMove;
            }

            if (_clickOrTap?.action != null)
            {
                _clickOrTap.action.Enable();
                _clickOrTap.action.performed += OnClickPerformed;
            }
        }

        private void OnDisable()
        {
            if (_pointerPosition?.action != null)
            {
                _pointerPosition.action.performed -= OnPointerMove;
                _pointerPosition.action.canceled -= OnPointerMove;
                _pointerPosition.action.Disable();
            }

            if (_clickOrTap?.action != null)
            {
                _clickOrTap.action.performed -= OnClickPerformed;
                _clickOrTap.action.Disable();
            }
        }
        private void OnPointerMove(InputAction.CallbackContext ctx)
        {
            if (!CanProcessInput()) return;
            _lastPointerPos = ctx.ReadValue<Vector2>();
            if (_uiOrthoCamera == null) return;

            Vector2 potentialRegion = Vector2.zero;
            if (_pointerPosition?.action != null)
            {
                potentialRegion = _pointerPosition.action.ReadValue<Vector2>();
            }

            bool inRegion = IsPointerInAnyRegion(potentialRegion, out int regionIndex);
            if (inRegion)
            {
                var potential = RaycastToPotentialTarget(_lastPointerPos, true, inRegion, regionIndex);
                if (potential != null)
                {
                    if (_lastProvider != null)
                    {
                        if (_lastProvider != potential)
                        {
                            //we changed
                            OnViewCubeUnHover?.Invoke(_lastProvider);
                            _lastProvider = potential;
                        }
                    }
                    else
                    {
                        //update
                        _lastProvider = potential;
                    }
                }
            }
            else if(_lastProvider!=null)
            {
                OnViewCubeUnHover?.Invoke(_lastProvider);
                _lastProvider = null;
            }
        }

        private void OnClickPerformed(InputAction.CallbackContext ctx)
        {
            if (!CanProcessInput()) return;
            if (_uiOrthoCamera == null) return;

            // If we don't have pointer updates via performed/canceled (some setups),
            // fall back to reading directly.
            if (_pointerPosition?.action != null)
                _lastPointerPos = _pointerPosition.action.ReadValue<Vector2>();

            bool inRegion = IsPointerInAnyRegion(_lastPointerPos, out int regionIndex);

            if (_requireRegion && !inRegion)
                return;

            var maybeSelection = RaycastToPotentialTarget(_lastPointerPos, false, inRegion, regionIndex);
            if (maybeSelection != null)
            {
                if (_lastSelected != null)
                {
                    if (_lastSelected != maybeSelection)
                    {
                        _lastSelected.UnSelect();
                        maybeSelection.Select();
                        _lastSelected = maybeSelection;
                    }
                }
                else
                {
                    _lastSelected = maybeSelection;
                }
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos">screen point</param>
        /// <param name="hover">are we clicking or just hover</param>
        /// <param name="inRegion">region check before function</param>
        /// <param name="regionIndex">region index</param>
        private FP_ViewCubeHitProvider RaycastToPotentialTarget(Vector2 pos,bool hover,bool inRegion,int regionIndex)
        {
            Ray ray = _uiOrthoCamera.ScreenPointToRay(pos);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _viewCubeLayerMask, QueryTriggerInteraction.Collide))
            {
                var provider = hit.collider.GetComponent<FP_ViewCubeHitProvider>();
                if (provider == null)
                {
                    // Optionally allow parent lookup if colliders are on children
                    provider = hit.collider.GetComponentInParent<FP_ViewCubeHitProvider>();
                }

                if (provider != null)
                {
                    if (_debugLogs)
                    {
                        string regionName = inRegion ? _regions.Region[regionIndex].Name : "(no region)";
                        Debug.Log($"[FP_ViewCubeUIRaycaster] Hit '{provider.HitType}' collider='{hit.collider.name}' region='{regionName}'");
                    }
                    if (!hover)
                    {
                        OnCubeSelect?.Invoke(provider, hit);
                    }
                    else
                    {
                        OnViewCubeHover?.Invoke(provider);
                    }
                    return provider;
                        
                }
                else if (_debugLogs)
                {
                    Debug.Log($"[FP_ViewCubeUIRaycaster] Ray hit collider='{hit.collider.name}' but no FP_ViewCubeHitProvider found.");
                }
            }
            else if (_debugLogs)
            {
                if (_requireRegion && inRegion)
                    Debug.Log("[FP_ViewCubeUIRaycaster] Click in region but raycast did not hit ViewCube.");
            }
            return null;
        }

        private bool IsPointerInAnyRegion(Vector2 screenPoint, out int regionIndex)
        {
            regionIndex = -1;

            if (_regions == null || _regions.Region.Length == 0)
                return !_requireRegion; // if no regions, treat as "allowed" only if not required

            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            for (int i = 0; i < _regions.Region.Length; i++)
            {
                if (_regions.Region[i].ContainsScreenPoint(screenPoint, screenSize))
                {
                    regionIndex = i;
                    return true;
                }
            }

            return false;
        }

        private bool CanProcessInput()
        {
            if (_requireApplicationFocus && !Application.isFocused)
                return false;

            return true;
        }
    */

    }
}
