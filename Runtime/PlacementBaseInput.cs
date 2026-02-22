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
        [SerializeField] protected InputState _state = InputState.Idle;
        [SerializeField] protected InputActionReference _pointerPosition;
        [SerializeField] protected InputActionReference _primaryClick;
        [SerializeField] protected Camera targetCamera;
        [Space]
        [Header("Raycasting")]
        [SerializeField] protected LayerMask placementMask;
        [Header("Input Region Gate")]
        [Tooltip("If true, this input behaviour only responds when the pointer/touch is inside the region.")]
        [SerializeField] protected bool _requireRegion = true;
        [Tooltip("Screen region that accepts orbit/zoom input.")]
        [SerializeField] protected FP_ScreenRegionAsset _inputRegion;
        [Space]
        [Header("Options")]
        [Tooltip("If true, orbit only occurs while the pointer is over the Game view (best effort).")]
        [SerializeField] protected bool _requireApplicationFocus = true;
        [SerializeField] protected bool _isDown;
        [Header("Input Lock")]
        [SerializeField] private bool _inputLocked;
        public bool IsInputLocked => _inputLocked;
        [Space]
        [Header("Click Settings")]
        [SerializeField] protected float doubleClickThreshold = 0.25f;
        [SerializeField] protected float dragStartThresholdPixels = 8f;
        [SerializeField] protected Vector2 _pressStartPos;
        [SerializeField] protected bool _useDistanceBasedClick = true;
        [Tooltip("To measure the delta from _pressStart to determine if we clicked or we are potentially doing something else ")]
        
        [SerializeField] protected Vector2 _endPressPos;

        [Space]
        [Header("Drag Timing")]
        [SerializeField] protected float dragSuppressTime = 0.12f; //seconds
        [SerializeField] protected bool _dragEligible;
        protected Coroutine _dragEligibilityRoutine;

        protected float _lastClickTime = -1f;
        protected int _clickCount = 0;

        protected Vector2 _lastPos;
        protected Vector3 _lastWorldPos;

        [SerializeField] Coroutine _clickResolutionRoutine;
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

            if(_state==InputState.Pressing && _dragEligible)
            {
                float dragDistance = Vector2.Distance(current, _pressStartPos);
                if(dragDistance>= dragStartThresholdPixels)
                {
                   
                    _state = InputState.Dragging;
                    OnDragStarted();
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
           
            _pressStartPos = _pointerPosition.action.ReadValue<Vector2>();
            if (!IsInRegion(_pressStartPos)) return;
            _isDown = true;

            if (_state == InputState.AwaitingSecondClick)
            {
                // SECOND CLICK
                _state = InputState.Idle;
                if (_clickResolutionRoutine != null)
                {
                    StopCoroutine(_clickResolutionRoutine);
                    _clickResolutionRoutine = null;
                }
                OnPrimaryDoubleClick(GetPointerWorldPosition());
                return;
            }

            // FIRST CLICK
            _state = InputState.Pressing;

            if (_dragEligibilityRoutine != null)
                StopCoroutine(_dragEligibilityRoutine);

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
            _endPressPos = _pointerPosition.action.ReadValue<Vector2>();
            if (_state == InputState.Pressing)
            {
                _state = InputState.AwaitingSecondClick;
                _clickResolutionRoutine = StartCoroutine(
                    ResolveClickAfterDelay(GetPointerWorldPosition())
                );
                return;
            }

            if (_state == InputState.Dragging)
            {
                OnDragEnded();
                _state = InputState.Idle;
            }
        }
        protected virtual bool CanProcessInput()
        {
            if (!_requireApplicationFocus) return true;
            return Application.isFocused;
        }
        protected virtual IEnumerator ResolveClickAfterDelay(Vector3 worldPos)
        {
            yield return new WaitForSeconds(doubleClickThreshold);
            if(_state == InputState.AwaitingSecondClick)
            {
                if (_useDistanceBasedClick&& Vector2.Distance(_pressStartPos, _endPressPos) <= dragStartThresholdPixels)
                {
                    OnPrimaryClick(worldPos);
                }
                else
                {
                                      
                    OnPrimaryClick(worldPos);
                }


                    _state = InputState.Idle;
            }
            _clickResolutionRoutine = null;
        }
        protected abstract void UpdateLogic();
        protected abstract void OnDragStarted();
        protected abstract void OnDragEnded();
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
                if (_isDown) ForceRelease();
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
            _dragEligible = false;
            if (_dragEligibilityRoutine != null)
            {
                StopCoroutine(_dragEligibilityRoutine);
                _dragEligibilityRoutine = null;
            }
        }
    }
}
