namespace FuzzPhyte.Placement
{
    using FuzzPhyte.Utility;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.EnhancedTouch;
    using System.Collections;

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

        [Space]
        [Header("Click Settings")]
        [SerializeField] protected float doubleClickThreshold = 0.25f;
        [SerializeField] protected float dragStartThresholdPixels = 8f;
        [SerializeField] protected Vector2 _pressStartPos;

        [Space]
        [Header("Drag Timing")]
        [SerializeField] protected float dragSuppressTime = 0.12f; //seconds
        [SerializeField] protected bool _dragEligible;
        protected Coroutine _dragEligibilityRoutine;

        protected float _lastClickTime = -1f;
        protected int _clickCount = 0;
        [SerializeField]protected bool _dragOccurred = false;
        protected bool _clickResolvedThisRelease;

        public bool IsInputLocked => _inputLocked;
        protected Vector2 _lastPos;
        protected bool _startedThisFrame;
        [SerializeField] protected bool _releasedThisFrame;
        
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
            if(_isDown && !_dragOccurred && _dragEligible)
            {
                float dragDistance = Vector2.Distance(current, _pressStartPos);

                if(dragDistance>= dragStartThresholdPixels)
                {
                    _dragOccurred = true;
                    _clickCount = 0;
                }
            }
            
            _lastPos = current;
            UpdateLogic();
        }
        #region Public Accessors
        public virtual void InputLocked()
        {
            SetInputLocked(true);
        }
        public virtual void InputUnlocked()
        {
            SetInputLocked(false);
        }
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
            _dragOccurred = false;
            _dragEligible = false;
            float time = Time.time;
            if(time - _lastClickTime <= doubleClickThreshold)
            {
                _clickCount++;
            }
            else
            {
                _clickCount = 1;
            }
            
            _lastClickTime = time;
            _pressStartPos = _pointerPosition.action.ReadValue<Vector2>();
            _lastPos = _pointerPosition.action.ReadValue<Vector2>();
            _clickResolvedThisRelease = false;
            if (_clickCount >= 2)
            {
                _dragEligible = false;
                return;
            }
            if (_dragEligibilityRoutine != null)
            {
                StopCoroutine(_dragEligibilityRoutine);
            }
            _dragEligibilityRoutine = StartCoroutine(EnableDragAfterDelay());

        }
        protected virtual IEnumerator EnableDragAfterDelay()
        {
            yield return new WaitForSeconds(dragSuppressTime);
            _dragEligible = true;
        }
        protected virtual void OnPrimaryUp(InputAction.CallbackContext ctx)
        {
            _isDown = false;
            _releasedThisFrame = true;
            _startedThisFrame = false;
            _dragEligible = false;
            Debug.Log($"Primary Up");
            if (_dragEligibilityRoutine!=null)
            {
                StopCoroutine(_dragEligibilityRoutine);
                _dragEligibilityRoutine = null;
            }
        }
        protected virtual bool CanProcessInput()
        {
            if (!_requireApplicationFocus) return true;
            return Application.isFocused;
        }
        
        protected abstract void UpdateLogic();
        /// <summary>
        /// Call before additional logic code as needed to confirm primary or double click
        /// </summary>
        protected virtual void ResolveClickIfNeeded()
        {
            if (!_releasedThisFrame) return;
            if (_clickResolvedThisRelease) return;

            _clickResolvedThisRelease = true;

            if (_dragOccurred)
                return; // drag wins over click

            Vector3 worldPos = GetPointerWorldPosition();

            if (_clickCount >= 2)
            {
                OnPrimaryDoubleClick(worldPos);
                _clickCount = 0;
            }
            else
            {
                OnPrimaryClick(worldPos);
            }
        }
        protected virtual Vector3 GetPointerWorldPosition()
        {
            Vector2 screenPos = _pointerPosition.action.ReadValue<Vector2>();
            Ray ray = targetCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementMask))
                return hit.point;

            return ray.origin + ray.direction * 10f;
        }
        #region Extra Actions
        /// <summary>
        /// Called when a primary double-click / double-tap is detected
        /// and no drag has occurred.
        /// </summary>
        protected virtual void OnPrimaryDoubleClick(Vector3 worldPos){}
        /// <summary>
        /// Called when a primary click has occurred and no drag has occurred
        /// </summary>
        /// <param name="worldPos"></param>
        protected virtual void OnPrimaryClick(Vector3 worldPos) {}
        #endregion
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
            _dragEligible = false;
            if (_dragEligibilityRoutine != null)
            {
                StopCoroutine(_dragEligibilityRoutine);
                _dragEligibilityRoutine = null;
            }
        }
    }
}
