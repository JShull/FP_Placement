namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    /// <summary>
    /// Mouse adapter for FP orbital camera using Unity's New Input System.
    /// - LMB press: begin orbit
    /// - Drag: orbit (delta from pointer movement)
    /// - LMB release: end orbit
    ///
    /// This class does not implement UI or gesture recognition; it simply converts mouse input to FP_OrbitalInput.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FP_OrbitalMouseInputBehaviour : MonoBehaviour
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

        [Tooltip("If true, inverts scroll direction.")]
        [SerializeField] private bool _invertScroll;

        [Header("Options")]
        [Tooltip("If true, orbit only occurs while the pointer is over the Game view (best effort).")]
        [SerializeField] private bool _requireApplicationFocus = true;

        private bool _isDown;
        private Vector2 _lastPos;
        private bool _startedThisFrame;
        private bool _releasedThisFrame;

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
        }

        private void OnDisable()
        {
            if (_primaryClick?.action != null)
            {
                _primaryClick.action.performed -= OnPrimaryDown;
                _primaryClick.action.canceled -= OnPrimaryUp;
                _primaryClick.action.Disable();
            }

            if (_pointerPosition?.action != null) _pointerPosition.action.Disable();
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
            if (!CanProcessInput()) return;

            _isDown = false;
            _releasedThisFrame = true;
            _startedThisFrame = false;
        }

        private void Update()
        {
            if (_orbital == null) return;
            if (!CanProcessInput()) return;
            if (_pointerPosition?.action == null) return;

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
            if (_scrollWheel?.action != null)
            {
                Vector2 scroll = _scrollWheel.action.ReadValue<Vector2>(); // x=horizontal, y=vertical
                float signed = scroll.y * _scrollToZoomScale;
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
                pinchDelta: 0f,
                isTwoFinger: false
            );

            _orbital.FeedInput(input);

            // Clear one-frame flags.
            _startedThisFrame = false;
            _releasedThisFrame = false;
        }

        private bool CanProcessInput()
        {
            if (!_requireApplicationFocus) return true;
            return Application.isFocused;
        }
    }
}
