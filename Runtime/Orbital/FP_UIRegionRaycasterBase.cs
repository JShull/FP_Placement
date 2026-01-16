namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using FuzzPhyte.Utility;

    /// <summary>
    /// Shared UI-region gated raycaster for 3D overlay UI objects.
    ///
    /// Flow:
    /// 1) Pointer move updates last pointer pos and optional hover
    /// 2) Click/tap performs a raycast (if region-gated) and dispatches selection
    ///
    /// Derived classes decide:
    /// - How to resolve provider from collider (default: GetComponent + GetComponentInParent)
    /// - What to do on hover/unhover/select (events, visuals, etc.)
    /// </summary>
    public abstract class FP_UIRegionRaycasterBase<TProvider> : MonoBehaviour where TProvider:Component
    {
        [Header("UI Ortho Camera")]
        [SerializeField] protected Camera _uiOverlayCamera;
        public Camera RaycastCamera => _uiOverlayCamera;

        [Header("Regions (screen space)")]
        [SerializeField] protected FP_ScreenRegionAsset _regions;

        [Header("Input Actions (New Input System)")]
        [Tooltip("Value/Vector2 - bind to <Pointer>/position")]
        [SerializeField] protected InputActionReference _pointerPosition;

        [Tooltip("Button - bind to <Mouse>/leftButton and/or <Touchscreen>/primaryTouch/press")]
        [SerializeField] protected InputActionReference _clickOrTap;

        [Header("Raycast Settings")]
        [SerializeField] protected LayerMask _layerMask = ~0;
        [SerializeField] protected float _maxDistance = 100f;

        [Header("Options")]
        [SerializeField] protected bool _requireApplicationFocus = true;
        [SerializeField] protected bool _requireRegion = true;
        [SerializeField] protected bool _enableHover = true;
        [SerializeField] protected bool _debugLogs;

        // cached
        [SerializeField]protected Vector2 _lastPointerPos;
        public Vector2 LastPointerPos => _lastPointerPos;
        protected TProvider _lastHoverProvider;
        [SerializeField]protected TProvider _lastSelectedProvider;

        protected virtual void OnEnable()
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

        protected virtual void OnDisable()
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

            // Clean hover state
            if (_lastHoverProvider != null)
            {
                OnUnHover(_lastHoverProvider);
                _lastHoverProvider = null;
            }
        }

        protected virtual void OnPointerMove(InputAction.CallbackContext ctx)
        {
            if (!CanProcessInput()) return;
            if (_uiOverlayCamera == null) return;

            _lastPointerPos = ctx.ReadValue<Vector2>();

            if (!_enableHover) return;

            bool inRegion = IsPointerInAnyRegion(_lastPointerPos, out int regionIndex);
            if (_requireRegion && !inRegion)
            {
                ClearHoverIfNeeded();
                return;
            }

            if (TryRaycast(_lastPointerPos, inRegion, regionIndex, out TProvider provider, out RaycastHit hit))
            {
                if (_lastHoverProvider != provider)
                {
                    if (_lastHoverProvider != null) OnUnHover(_lastHoverProvider);
                    _lastHoverProvider = provider;
                    OnHover(provider, hit);
                }
            }
            else
            {
                ClearHoverIfNeeded();
            }
        }

        protected virtual void OnClickPerformed(InputAction.CallbackContext ctx)
        {
            if (!CanProcessInput()) return;
            if (_uiOverlayCamera == null) return;

            //context in this case is a button action and we need the other vector2 location action from _pointerPosition
            _lastPointerPos = _pointerPosition.action.ReadValue<Vector2>();

            bool inRegion = IsPointerInAnyRegion(_lastPointerPos, out int regionIndex);
            if (_requireRegion && !inRegion) return;

            if (TryRaycast(_lastPointerPos, inRegion, regionIndex, out TProvider provider, out RaycastHit hit))
            {
                // Selection bookkeeping (optional)
                if (_lastSelectedProvider != null && _lastSelectedProvider != provider)
                    OnUnSelect(_lastSelectedProvider);

                _lastSelectedProvider = provider;
                OnSelect(provider, hit);
            }
            else if (_debugLogs && (!_requireRegion || inRegion))
            {
                Debug.Log($"[{GetDebugTag()}] Click in region but raycast did not hit.");
            }
        }

        protected virtual bool TryRaycast(
            Vector2 screenPos,
            bool inRegion,
            int regionIndex,
            out TProvider provider,
            out RaycastHit hit)
        {
            provider = null;
            hit = default;

            Ray ray = _uiOverlayCamera.ScreenPointToRay(screenPos);
            if (_debugLogs)
            {
                Debug.DrawRay(ray.origin, ray.direction * _maxDistance, Color.cyan, 3f, false);
            }
            if (!Physics.Raycast(ray, out hit, _maxDistance, _layerMask, QueryTriggerInteraction.Collide))
            {
                return false;
            }

            provider = ResolveProvider(hit);
            if (provider == null)
            {
                if (_debugLogs)
                    Debug.Log($"[{GetDebugTag()}] Ray hit collider='{hit.collider.name}' but no {typeof(TProvider).Name} found.");
                return false;
            }

            if (_debugLogs)
            {
                string regionName = (inRegion && _regions != null && _regions.Region != null && regionIndex >= 0 && regionIndex < _regions.Region.Length)
                    ? _regions.Region[regionIndex].Name
                    : "(no region)";

                Debug.Log($"[{GetDebugTag()}] Hit provider='{provider.name}' collider='{hit.collider.name}' region='{regionName}'");
            }

            return true;
        }

        protected virtual TProvider ResolveProvider(in RaycastHit hit)
        {
            // default resolution matches your current raycasters
            var p = hit.collider.GetComponent<TProvider>();
            return p != null ? p : hit.collider.GetComponentInParent<TProvider>();
        }

        protected virtual bool IsPointerInAnyRegion(Vector2 screenPoint, out int regionIndex)
        {
            regionIndex = -1;

            if (_regions == null || _regions.Region == null || _regions.Region.Length == 0)
                return !_requireRegion;

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

        protected virtual bool CanProcessInput()
        {
            if (_requireApplicationFocus && !Application.isFocused)
                return false;
            return true;
        }

        protected void ClearHoverIfNeeded()
        {
            if (_lastHoverProvider != null)
            {
                OnUnHover(_lastHoverProvider);
                _lastHoverProvider = null;
            }
        }

        // Hooks for derived classes
        protected abstract string GetDebugTag();
        protected abstract void OnSelect(TProvider provider, RaycastHit hit);

        // Optional hooks
        protected virtual void OnHover(TProvider provider, RaycastHit hit) { }
        protected virtual void OnUnHover(TProvider provider) { }
        protected virtual void OnUnSelect(TProvider provider) { }
    }
}
