namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using FuzzPhyte.Utility;
    //touch
    using UnityEngine.InputSystem.EnhancedTouch;
    using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
    /// <summary>
    /// Mouse adapter for FP orbital camera using Unity's New Input System.
    /// - LMB press: begin orbit
    /// - Drag: orbit (delta from pointer movement)
    /// - LMB release: end orbit
    ///
    /// This class does not implement UI or gesture recognition; it simply converts mouse input to FP_OrbitalInput.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class FP_OrbitalMouseInputBehaviour : MonoBehaviour
    {
        [Header("Target Orbital Camera")]
        [SerializeField] private FP_OrbitalCameraBehaviour _orbital;

        [Header("Mouse Mode")]
        [Tooltip("System starts in Orbit Mode")]
        [SerializeField] private FP_OrbitalMouseMode _mode = FP_OrbitalMouseMode.Orbit;

        [Header("Input Actions (New Input System)")]
        [Tooltip("Value/Vector2 - typically bound to <Pointer>/position")]
        [SerializeField] private InputActionReference _pointerPosition;

        [Tooltip("Button - typically bound to <Mouse>/leftButton")]
        [SerializeField] private InputActionReference _primaryClick;

        [Tooltip("Value/Vector2 - typically bound to <Mouse>/scroll")]
        [SerializeField] private InputActionReference _scrollWheel;

        [Header("Pan Options")]
        [Tooltip("Button - typically bound to <Mouse>/MiddleButton")]
        [SerializeField] private InputActionReference _middleMouse;
        private bool _isPanDown;
        private bool _panStartedThisFrame;
        private bool _panReleasedThisFrame;
        private Vector2 _lastPanPos;
        [Header("Scroll Options")]
        [Tooltip("Scales <Mouse>/scroll to a usable zoom delta. Tune to taste.")]
        [SerializeField] private float _scrollToZoomScale = 0.02f;

        [Tooltip("Scales pinch distance delta (in pixels) to pinchDelta used by the orbital controller.")]
        [SerializeField] private float _pinchToZoomScale = 0.0025f;
        [SerializeField] private bool _invertPinch;

        [Tooltip("If true, inverts scroll direction.")]
        [SerializeField] private bool _invertScroll;

        [Header("Options")]
        [Tooltip("If true, orbit only occurs while the pointer is over the Game view (best effort).")]
        [SerializeField] private bool _requireApplicationFocus = true;

        [Header("Input Lock")]
        [SerializeField] private bool _inputLocked;
        public bool IsInputLocked => _inputLocked;

        [SerializeField]private bool _isDown;
        private Vector2 _lastPos;
        private bool _startedThisFrame;
        private bool _releasedThisFrame;
        [Tooltip("If true, the mouse mode (orbit/pan) will switch dynamically based on which button is held down.")]
        [SerializeField]private bool hybridMode=true;
        [SerializeField]private bool iPadTouchMode=false;

        [Header("Input Region Gate")]
        [Tooltip("If true, this input behaviour only responds when the pointer/touch is inside the region.")]
        [SerializeField] private bool _requireRegion = true;

        [Tooltip("Screen region that accepts orbit/zoom input.")]
        [SerializeField] private FP_ScreenRegionAsset _inputRegion;

        // Scroll is a delta stream; we accumulate and consume once per frame.
        private float _scrollAccumY;
        private bool _wasOneFingerDown;
        private bool _wasTwoFingerDown;

        private Vector2 _lastOnePos;
        private float _lastTwoFingerDistance;
        private Vector2 _lastTwoFingerCenter;

        //force input from jumping
        private bool _suppressOrbitDelta;
        private bool _suppressPanDelta;


        #region Testing Methods
        [ContextMenu("Lock Mouse Input")]
        public void LockMouseInput()
        {
            SetInputLocked(true);
        }
        [ContextMenu("Unlock Mouse Input")]
        public void UnlockMouseInput()
        {
            SetInputLocked(false);
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// If locked, this adapter will ignore all input and will cancel any active drag.
        /// Intended for UI/tools to temporarily disable camera motion.
        /// </summary>
        public void SetInputLocked(bool locked)
        {
            if (_inputLocked == locked) return;

            _inputLocked = locked;

            // If we lock while dragging, force a release so the camera stops immediately.
            if (_inputLocked && _isDown)
            {
                ForceRelease();
            }
            if(_inputLocked && _isPanDown)
            {
                ForcePanRelease();
            }
        }
        public void SetMode(FP_OrbitalMouseMode mode)
        {
            if (mode == _mode) return;
            _mode = mode;

            CancelAllInputState();
        }
        public void RecenterBounds()
        {
            _orbital.RecenterToTargetBounds(false);
        }
        #endregion
        private void Reset()
        {
            _orbital = GetComponent<FP_OrbitalCameraBehaviour>();
        }
        private void Awake()
        {
            if (_orbital == null)
                _orbital = GetComponent<FP_OrbitalCameraBehaviour>();
        }
        private void OnEnable()
        {
            if (_pointerPosition?.action != null) _pointerPosition.action.Enable();

            if (_primaryClick?.action != null)
            {
                _primaryClick.action.Enable();
                _primaryClick.action.performed += OnPrimaryDown;
                _primaryClick.action.canceled += OnPrimaryUp;
            }
            if (_scrollWheel?.action != null)
            {
                _scrollWheel.action.Enable();
                _scrollWheel.action.performed += OnScrollPerformed;
                _scrollWheel.action.canceled += OnScrollPerformed; // some devices emit cancel; safe to listen
            }
            if (_middleMouse?.action != null)
            {
                _middleMouse.action.Enable();
                _middleMouse.action.performed += OnMiddleDown;
                _middleMouse.action.canceled += OnMiddleUp;
            }
            EnhancedTouchSupport.Enable();
        }
        private void OnDisable()
        {
            if (_primaryClick?.action != null)
            {
                _primaryClick.action.performed -= OnPrimaryDown;
                _primaryClick.action.canceled -= OnPrimaryUp;
                _primaryClick.action.Disable();
            }
             if (_scrollWheel?.action != null)
            {
                _scrollWheel.action.performed -= OnScrollPerformed;
                _scrollWheel.action.canceled -= OnScrollPerformed;
                _scrollWheel.action.Disable();
            }
            if (_middleMouse?.action != null)
            {
                _middleMouse.action.performed -= OnMiddleDown;
                _middleMouse.action.canceled -= OnMiddleUp;
                _middleMouse.action.Disable();
            }
            if (_pointerPosition?.action != null) _pointerPosition.action.Disable();
            EnhancedTouchSupport.Disable();
            _wasOneFingerDown = false;
            _wasTwoFingerDown = false;
            if (_isDown)
            {
                 ForceRelease();
            }
            if (_isPanDown)
            {
                ForcePanRelease();
            }
        }

        private void Update()
        {
            if (_orbital == null) return;
            if (!CanProcessInput()) return;
            if (_pointerPosition?.action == null) return;

            // If locked: cancel any active gesture and ignore deltas
            if (_inputLocked)
            {
                if (_isDown) ForceRelease();
                _wasOneFingerDown = false;
                _wasTwoFingerDown = false;
                _startedThisFrame = false;
                _releasedThisFrame = false;
                _scrollAccumY = 0f;

                if (_isPanDown) ForcePanRelease();
                _panReleasedThisFrame = false;
                _panStartedThisFrame = false;
                return;
            }
            // NEW Zoom Touch

            // ------------------------------------------------------------
            // 2-FINGER TOUCH ONLY: pinch zoom (special case)
            // ------------------------------------------------------------
            int touchCount = Touch.activeTouches.Count;

            if (touchCount >= 2)
            {
                // During pinch, prevent any one-pointer gesture from sticking.
                if (_isDown) ForceRelease();
                if (_isPanDown) ForcePanRelease();

                // Read the first two touches
                var t0 = Touch.activeTouches[0];
                var t1 = Touch.activeTouches[1];

                Vector2 p0 = t0.screenPosition;
                Vector2 p1 = t1.screenPosition;

                Vector2 center = (p0 + p1) * 0.5f;
                float dist = Vector2.Distance(p0, p1);

                // Region gate: use pinch center
                if (!IsInRegion(center))
                {
                    CancelTouchStateOnly();
                    _scrollAccumY = 0f;
                    return;
                }

                bool pressedThisFrame = !_wasTwoFingerDown;

                bool anyEnded =
                    t0.phase == UnityEngine.InputSystem.TouchPhase.Ended || t0.phase == UnityEngine.InputSystem.TouchPhase.Canceled ||
                    t1.phase == UnityEngine.InputSystem.TouchPhase.Ended || t1.phase == UnityEngine.InputSystem.TouchPhase.Canceled;

                bool releasedThisFrame = _wasTwoFingerDown && anyEnded;

                float pinchDelta = 0f;
                if (_wasTwoFingerDown)
                {
                    float deltaDist = dist - _lastTwoFingerDistance; // + = fingers moving apart
                    float signed = deltaDist * _pinchToZoomScale;
                    pinchDelta = _invertPinch ? -signed : signed;
                }

                _lastTwoFingerDistance = dist;
                _lastTwoFingerCenter = center;

                _wasTwoFingerDown = !anyEnded;
                _wasOneFingerDown = false;

                // Feed zoom-only (no pan on pinch)
                _orbital.FeedInput(new FP_OrbitalInput(
                    isPressed: pressedThisFrame,
                    isReleased: releasedThisFrame,
                    pointerPos: center,
                    dragDelta: Vector2.zero,
                    pinchDelta: pinchDelta,
                    isTwoFinger: true
                ));

                _scrollAccumY = 0f;
                return;
            }
            else
            {
                // If we just left a pinch gesture, send a release once.
                if (_wasTwoFingerDown)
                {
                    _wasTwoFingerDown = false;

                    _orbital.FeedInput(new FP_OrbitalInput(
                        isPressed: false,
                        isReleased: true,
                        pointerPos: Vector2.zero,
                        dragDelta: Vector2.zero,
                        pinchDelta: 0f,
                        isTwoFinger: false
                    ));
                }
            }

            
            // --- MOUSE MODE (Input Actions) ---
            Vector2 current = _pointerPosition.action.ReadValue<Vector2>();

            // region gate

            if (!IsInRegion(current))
            {
                _scrollAccumY = 0;
                // primary mouse orbit input parameters
                if (_isDown) ForceRelease();
                _startedThisFrame = false;
                _releasedThisFrame = false;

                // middle mouse pan input parameters
                if (_isPanDown) ForcePanRelease();
                _panStartedThisFrame = false;
                _panReleasedThisFrame = false;
                return;
            }

            // zoom related
            float pinchDeltaMouse = 0f;
            if (Mathf.Abs(_scrollAccumY) > Mathf.Epsilon)
            {
                float signed = _scrollAccumY * _scrollToZoomScale;
                pinchDeltaMouse = _invertScroll ? -signed : signed;
            }

            // New hybrid mode: determine effective mode
            FP_OrbitalMouseMode effectiveMode = _mode;
            // break if our mode is none
            if (effectiveMode == FP_OrbitalMouseMode.None)
            {
                _scrollAccumY = 0;
                // primary mouse orbit input parameters
                if (_isDown) ForceRelease();
                _startedThisFrame = false;
                _releasedThisFrame = false;

                // middle mouse pan input parameters
                if (_isPanDown) ForcePanRelease();
                _panStartedThisFrame = false;
                _panReleasedThisFrame = false;
                return;
            }
            if (hybridMode)
            {
                 // Momentary overrides: middle pan wins over left orbit
                if (_isPanDown) effectiveMode = FP_OrbitalMouseMode.Pan;
                else if (_isDown) effectiveMode = FP_OrbitalMouseMode.Orbit;
            }
            else
            {
                //do nothing - like for an iPad interface
            }
            
            if (effectiveMode == FP_OrbitalMouseMode.Orbit)
            {
                Vector2 delta = Vector2.zero;
                
                if (_isDown)
                {
                    if (_startedThisFrame || _suppressOrbitDelta)
                    {
                        // Prime the drag so the first frame never spikes.
                        _lastPos = current;
                        delta = Vector2.zero;
                        _suppressOrbitDelta = false;
                    }
                    else
                    {
                        delta = current - _lastPos;
                        _lastPos = current;
                    }
                    //OLD
                    //delta = current - _lastPos;
                    //_lastPos = current;
                }

                _orbital.FeedInput(new FP_OrbitalInput(
                    isPressed: _startedThisFrame,
                    isReleased: _releasedThisFrame,
                    pointerPos: current,
                    dragDelta: delta,
                    pinchDelta: pinchDeltaMouse,
                    isTwoFinger: false
                ));

                _startedThisFrame = false;
                _releasedThisFrame = false;
            }
            else // Pan
            {

                Vector2 panDelta = Vector2.zero;
                if (_isPanDown)
                {
                    if (_panStartedThisFrame || _suppressPanDelta)
                    {
                        _lastPanPos = current;
                        panDelta = Vector2.zero;
                        _suppressPanDelta = false;
                    }
                    else
                    {
                        panDelta = current - _lastPanPos;
                        _lastPanPos = current;
                    }
                    //OLD
                    //panDelta = current - _lastPanPos;
                    //_lastPanPos = current;
                }

                _orbital.FeedInput(new FP_OrbitalInput(
                    isPressed: _panStartedThisFrame,
                    isReleased: _panReleasedThisFrame,
                    pointerPos: current,
                    dragDelta: panDelta,
                    pinchDelta: pinchDeltaMouse,
                    isTwoFinger: true
                ));

                _panStartedThisFrame = false;
                _panReleasedThisFrame = false;
            }
            _scrollAccumY = 0f;
        }
        private void OnPrimaryDown(InputAction.CallbackContext ctx)
        {
            if (!CanProcessInput()) return;
            if (_pointerPosition?.action == null) return;
            if (_inputLocked) return;

            // mouse/PC mode
            if (iPadTouchMode && _mode == FP_OrbitalMouseMode.Pan)
            {
                // send the information over to middle mouse down if we are in pan mode
                OnMiddleDown(ctx);
                return;
            }
            // Ensure pan isn't active (LMB takes over)
            if (_isPanDown) ForcePanRelease();

            _isDown = true;
            _startedThisFrame = true;
            _releasedThisFrame = false;
            _lastPos = _pointerPosition.action.ReadValue<Vector2>();
            _suppressOrbitDelta = true;
            
        }
        private void OnPrimaryUp(InputAction.CallbackContext ctx)
        {
            // mouse/PC mode
            if (iPadTouchMode && _mode == FP_OrbitalMouseMode.Pan)
            {
                // send the information over to middle mouse down if we are in pan mode
                OnMiddleUp(ctx);
                return;
            }
            _isDown = false;
            _releasedThisFrame = true;
            _startedThisFrame = false;
        }
        private void OnScrollPerformed(InputAction.CallbackContext ctx)
        {
            if (!CanProcessInput() || _inputLocked) return;
            // Scroll is Vector2 delta (x horizontal, y vertical)
            Vector2 scroll = ctx.ReadValue<Vector2>();
            _scrollAccumY += scroll.y;
        }
        private void OnMiddleDown(InputAction.CallbackContext ctx)
        {
            if (!CanProcessInput()) return;
            if (_pointerPosition?.action == null) return;
            if (_inputLocked) return;

            _isPanDown = true;
            _panStartedThisFrame = true;
            _panReleasedThisFrame = false;
            _lastPanPos = _pointerPosition.action.ReadValue<Vector2>();
            _suppressPanDelta=true;
            // If orbit was active, stop orbit immediately (MMB takes over)
            if (_isDown) ForceRelease();
            
        }
        private void OnMiddleUp(InputAction.CallbackContext ctx)
        {
            if (_inputLocked) return;

            _isPanDown = false;
            _panReleasedThisFrame = true;
            _panStartedThisFrame = false;
        }
        private bool CanProcessInput()
        {
            if (!_requireApplicationFocus) return true;
            return Application.isFocused;
        }
        private bool IsInRegion(Vector2 screenPoint)
        {
            if (!_requireRegion) return true;

            // Assumes your global FP_ScreenRegion has a method like:
            // bool ContainsScreenPoint(Vector2 screenPoint, Vector2 screenSize)
            for(int i = 0; i < _inputRegion.Region.Length; i++)
            {
                var region = _inputRegion.Region[i];
                //Debug.Log($"Mouse01: {screenPoint.x}, {screenPoint.y} and Width/height = {Screen.width} / {Screen.height}");
                bool inRegion = region.ContainsScreenPoint(screenPoint, new Vector2(Screen.width, Screen.height));
                if(inRegion) return true;
            }
            return false;
        }
        private void CancelTouchStateOnly()
        {
            if (_wasOneFingerDown || _wasTwoFingerDown)
            {
                _wasOneFingerDown = false;
                _wasTwoFingerDown = false;

                _orbital.FeedInput(new FP_OrbitalInput(
                    isPressed: false,
                    isReleased: true,
                    pointerPos: Vector2.zero,
                    dragDelta: Vector2.zero,
                    pinchDelta: 0f,
                    isTwoFinger: false
                ));
            }
        }
        private void CancelAllInputState()
        {
            if (_isDown) ForceRelease();
            if (_isPanDown) ForcePanRelease();

            CancelTouchStateOnly();

            _startedThisFrame = false;
            _releasedThisFrame = false;
            _panReleasedThisFrame = false;
            _panStartedThisFrame = false;
            _scrollAccumY = 0f;
        }
        private void ForceRelease()
        {
            _isDown = false;
            _startedThisFrame = false;
            _releasedThisFrame = true;
            _suppressOrbitDelta = false;
            if (_orbital != null && _pointerPosition?.action != null)
            {
                Vector2 current = _pointerPosition.action.ReadValue<Vector2>();
                _orbital.FeedInput(new FP_OrbitalInput(
                    isPressed: false,
                    isReleased: true,
                    pointerPos: current,
                    dragDelta: Vector2.zero,
                    pinchDelta: 0f,
                    isTwoFinger: false
                ));
            }

            _releasedThisFrame = false;
        }
        private void ForcePanRelease()
        {
            _isPanDown = false;
            _panStartedThisFrame = false;
            _panReleasedThisFrame = true;
            _suppressPanDelta = false;
            if (_orbital != null && _pointerPosition?.action != null)
            {
                Vector2 current = _pointerPosition.action.ReadValue<Vector2>();
                _orbital.FeedInput(new FP_OrbitalInput(
                    isPressed: false,
                    isReleased: true,
                    pointerPos: current,
                    dragDelta: Vector2.zero,
                    pinchDelta: 0f,
                    isTwoFinger: true
                ));
            }

            _panReleasedThisFrame = false;
        }
    }
}
