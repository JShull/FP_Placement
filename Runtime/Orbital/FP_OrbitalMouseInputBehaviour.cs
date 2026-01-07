namespace FuzzPhyte.Placement.OrbitalCamera
{
#if UNITY_EDITOR
    using UnityEditor;
#endif
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

        [Header("Input Actions (New Input System)")]
        [Tooltip("Value/Vector2 - typically bound to <Pointer>/position")]
        [SerializeField] private InputActionReference _pointerPosition;

        [Tooltip("Button - typically bound to <Mouse>/leftButton")]
        [SerializeField] private InputActionReference _primaryClick;

        [Tooltip("Value/Vector2 - typically bound to <Mouse>/scroll")]
        [SerializeField] private InputActionReference _scrollWheel;

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
            if (_pointerPosition?.action != null) _pointerPosition.action.Disable();
            EnhancedTouchSupport.Disable();
            _wasOneFingerDown = false;
            _wasTwoFingerDown = false;
            if (_isDown)
            {
                 ForceRelease();
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
                return;
            }

            // --- TOUCH MODE (EnhancedTouch) ---
            int count = Touch.activeTouches.Count;
            if (count > 0)
            {
                // Prevent mouse drag from "sticking" if a touch begins
                if (_isDown) ForceRelease();
                _scrollAccumY = 0f;

                // 1 finger orbit
                if (count == 1)
                {
                    var t0 = Touch.activeTouches[0];
                    Vector2 pos = t0.screenPosition;

                    if (!IsInRegion(pos))
                    {
                        CancelTouchStateOnly();
                        return;
                    }
                    bool pressedThisFrame = (!_wasOneFingerDown && t0.phase == UnityEngine.InputSystem.TouchPhase.Began);

                    bool releasedThisFrame = (_wasOneFingerDown &&
                                                (t0.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                                                t0.phase == UnityEngine.InputSystem.TouchPhase.Canceled));

                    Vector2 dragDelta = Vector2.zero;
                    if (_wasOneFingerDown &&
                        (t0.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                            t0.phase == UnityEngine.InputSystem.TouchPhase.Stationary))
                    {
                        dragDelta = pos - _lastOnePos;
                    }

                    _lastOnePos = pos;
                    _wasOneFingerDown = !(t0.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                                            t0.phase == UnityEngine.InputSystem.TouchPhase.Canceled);

                    // Dropping from 2 fingers to 1 finger resets pinch state
                    _wasTwoFingerDown = false;

                    _orbital.FeedInput(new FP_OrbitalInput(
                        isPressed: pressedThisFrame,
                        isReleased: releasedThisFrame,
                        pointerPos: pos,
                        dragDelta: dragDelta,
                        pinchDelta: 0f,
                        isTwoFinger: false
                    ));

                    return;
                }

                // 2+ fingers: pinch zoom (and optionally pan if you later decide)
                {
                    var t0 = Touch.activeTouches[0];
                    var t1 = Touch.activeTouches[1];

                    Vector2 p0 = t0.screenPosition;
                    Vector2 p1 = t1.screenPosition;

                    Vector2 center = (p0 + p1) * 0.5f;
                    float dist = Vector2.Distance(p0, p1);

                    if (!IsInRegion(center))
                    {
                        CancelTouchStateOnly();
                        return;
                    }
                    bool pressedThisFrame = !_wasTwoFingerDown;

                    // With EnhancedTouch, "release" is better handled when touches drop to 0,
                    // but we still allow a release when one of the two touches ends.
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

                    _orbital.FeedInput(new FP_OrbitalInput(
                        isPressed: pressedThisFrame,
                        isReleased: releasedThisFrame,
                        pointerPos: center,
                        dragDelta: Vector2.zero,      // (optional later: set to center - _lastTwoFingerCenter for pan)
                        pinchDelta: pinchDelta,
                        isTwoFinger: true
                    ));

                    return;
                }
            }
            else
            {
                // No touches: if we previously had touches, send a release once so the camera stops cleanly.
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

            // --- MOUSE MODE (Input Actions) ---
            Vector2 current = _pointerPosition.action.ReadValue<Vector2>();

            // region gate

            if (!IsInRegion(current))
            {
                _scrollAccumY = 0;
                if (_isDown)
                {
                    ForceRelease();
                }
                _startedThisFrame = false;
                _releasedThisFrame = false;
                return;
            }

            Vector2 delta = Vector2.zero;
            if (_isDown)
            {
                delta = current - _lastPos;
                _lastPos = current;
            }

            float pinchDeltaMouse = 0f;
            if (Mathf.Abs(_scrollAccumY) > Mathf.Epsilon)
            {
                float signed = _scrollAccumY * _scrollToZoomScale;
                pinchDeltaMouse = _invertScroll ? -signed : signed;
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
            _scrollAccumY = 0f;
            #region Old Update Logic Before Touch
            /*
            if (_orbital == null) return;
            if (!CanProcessInput()) return;
            if (_pointerPosition?.action == null) return;

            // If locked: do nothing (but ensure our internal flags don’t accumulate)
            if (_inputLocked)
            {
                _startedThisFrame = false;
                _releasedThisFrame = false;
                 _scrollAccumY = 0f;
                return;
            }
            // process touch here?
            int count = Touch.activeTouches.Count;


            Vector2 current = _pointerPosition.action.ReadValue<Vector2>();

            // Orbit drag delta (pixels)
            Vector2 delta = Vector2.zero;
            if (_isDown)
            {
                delta = current - _lastPos;
                _lastPos = current;
            }
            // Scroll zoom -> pinchDelta
            // <Mouse>/scroll is typically "lines" (often +/-120-ish per notch on Windows), but varies by device.
            // We scale it down to something stable.
            float pinchDelta = 0f;
            if (Mathf.Abs(_scrollAccumY) > Mathf.Epsilon)
            {
                float signed = _scrollAccumY * _scrollToZoomScale;
                pinchDelta = _invertScroll ? -signed : signed;
                
            }

            // Build the packet:
            // - IsPressed only true on the first frame of down.
            // - IsReleased only true on the first frame of up.
            // - DragDelta only meaningful while held.
            var input = new FP_OrbitalInput(
                isPressed: _startedThisFrame,
                isReleased: _releasedThisFrame,
                pointerPos: current,
                dragDelta: delta,
                pinchDelta: pinchDelta,
                isTwoFinger: false
            );
            // Consume scroll for this frame
            
            _orbital.FeedInput(input);

            // Clear one-frame flags.
            _startedThisFrame = false;
            _releasedThisFrame = false;
            _scrollAccumY = 0f;
            */
            #endregion
        }

        private void OnPrimaryDown(InputAction.CallbackContext ctx)
        {
            if (!CanProcessInput()) return;
            if (_pointerPosition?.action == null) return;

            _isDown = true;
            _startedThisFrame = true;
            _releasedThisFrame = false;
            _lastPos = _pointerPosition.action.ReadValue<Vector2>();
        }

        private void OnPrimaryUp(InputAction.CallbackContext ctx)
        {
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

            CancelTouchStateOnly();

            _startedThisFrame = false;
            _releasedThisFrame = false;
            _scrollAccumY = 0f;
        }
        private void ForceRelease()
        {
            _isDown = false;
            _startedThisFrame = false;
            _releasedThisFrame = true;

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
    }
   
}
