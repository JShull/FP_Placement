namespace FuzzPhyte.Placement
{
    using FuzzPhyte.Utility;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.EnhancedTouch;

    public abstract class PlacementBaseInput : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] protected InputActionReference _pointerPosition;
        [SerializeField] protected InputActionReference _primaryClick;

        [SerializeField] protected Camera targetCamera;

        [Header("Raycasting")]
        [SerializeField] protected LayerMask placementMask;
        [Header("Input Region Gate")]
        [Tooltip("If true, this input behaviour only responds when the pointer/touch is inside the region.")]
        [SerializeField] protected bool _requireRegion = true;
        [Tooltip("Screen region that accepts orbit/zoom input.")]
        [SerializeField] protected FP_ScreenRegionAsset _inputRegion;

        [Header("Options")]
        [Tooltip("If true, orbit only occurs while the pointer is over the Game view (best effort).")]
        [SerializeField] protected bool _requireApplicationFocus = true;
        [SerializeField] protected bool _isDown;
        [Header("Input Lock")]
        [SerializeField] private bool _inputLocked;
        public bool IsInputLocked => _inputLocked;
        protected Vector2 _lastPos;
        protected bool _startedThisFrame;
        protected bool _releasedThisFrame;
        
        public virtual void OnEnable()
        {
            if (_pointerPosition?.action != null) _pointerPosition.action.Enable();

            if (_primaryClick?.action != null)
            {
                _primaryClick.action.Enable();
                _primaryClick.action.performed += OnPrimaryDown;
                _primaryClick.action.canceled += OnPrimaryUp;
            }
            EnhancedTouchSupport.Enable();
        }
        public virtual void OnDisable()
        {
            if (_pointerPosition?.action != null) _pointerPosition.action.Disable();

            if (_primaryClick?.action != null)
            {
                _primaryClick.action.performed -= OnPrimaryDown;
                _primaryClick.action.canceled -= OnPrimaryUp;
                _primaryClick.action.Disable();
            }
            EnhancedTouchSupport.Disable();
        }
        
        public virtual void Update()
        {
            if (!CanProcessInput()) return;
            if (_pointerPosition?.action == null) return;
            if (_inputLocked)
            {
                ForceRelease();
                return;
            }
            Vector2 current = _pointerPosition.action.ReadValue<Vector2>();
            if (!RegionGate(current)) return;
            UpdateLogic();
        }
        #region Public Accessors
        public virtual void SetInputLocked(bool locked)
        {
            if (_inputLocked == locked) return;
            _inputLocked = locked;
            // If we lock while dragging force a release
            if (_inputLocked && _isDown)
            {
                ForceRelease();
            }
        }
        #endregion
        protected virtual void OnPrimaryDown(InputAction.CallbackContext ctx)
        {
            if (!CanProcessInput()) return;
            if (_pointerPosition?.action == null) return;
            if (_inputLocked)
            {
                ForceRelease();
                return;
            }
            _isDown = true;
            _startedThisFrame = true;
            _releasedThisFrame = false;
            _lastPos = _pointerPosition.action.ReadValue<Vector2>();
        }
        protected virtual void OnPrimaryUp(InputAction.CallbackContext ctx)
        {
            _isDown = false;
            _releasedThisFrame = true;
            _startedThisFrame = false;
        }
        protected virtual bool CanProcessInput()
        {
            if (!_requireApplicationFocus) return true;
            return Application.isFocused;
        }
        
        protected abstract void UpdateLogic();
        protected virtual bool RegionGate(Vector2 pointerPos)
        {
            if (!IsInRegion(pointerPos))
            {
                // primary mouse orbit input parameters
                if (_isDown) ForceRelease();
                _startedThisFrame = false;
                _releasedThisFrame = false;
                return false;
            }
            return true;
        }
        protected virtual bool IsInRegion(Vector2 screenPoint)
        {
            if (!_requireRegion) return true;

            for (int i = 0; i < _inputRegion.Region.Length; i++)
            {
                var region = _inputRegion.Region[i];
                bool inRegion = region.ContainsScreenPoint(screenPoint, new Vector2(Screen.width, Screen.height));
                if (inRegion) return true;
            }
            return false;
        }
        protected virtual void ForceRelease()
        {
            _isDown = false;
            _startedThisFrame = false;
            _releasedThisFrame = false;
        }
    }
}
