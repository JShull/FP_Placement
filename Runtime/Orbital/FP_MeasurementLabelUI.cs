namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using TMPro;
    using FuzzPhyte.Utility;

    /// <summary>
    /// Always-on-top measurement label (Screen Space Overlay).
    /// Reads A/B from FPRuntimeMeasurementOverlay and positions a TMPUGUI label
    /// at the midpoint projected into screen space.
    /// </summary>
    public sealed class FP_MeasurementLabelUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private FPRuntimeMeasurementOverlay _overlay;
        [SerializeField] private TMP_Text _label; // TextMeshProUGUI or TMP_Text

        [Header("Layout")]
        [Tooltip("Extra pixel offset applied after projecting midpoint into screen space.")]
        [SerializeField] private Vector2 _screenOffset = new Vector2(0f, 18f);

        [Tooltip("Clamp label to screen bounds (keeps it visible even at edges).")]
        [SerializeField] private bool _clampToScreen = true;

        [Tooltip("Padding (pixels) used when clamping to screen.")]
        [SerializeField] private Vector2 _screenPadding = new Vector2(8f, 8f);

        private RectTransform _rect;

        private void Awake()
        {
            _rect = transform as RectTransform;

            if (_overlay == null)
                _overlay = FPRuntimeMeasurementOverlay.Active;

            if (_worldCamera == null)
                _worldCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_label == null)
                return;

            if (_overlay == null)
                _overlay = FPRuntimeMeasurementOverlay.Active;

            if (_overlay == null || !_overlay.HasMeasurement)
            {
                if (_label.gameObject.activeSelf) _label.gameObject.SetActive(false);
                return;
            }

            if (_worldCamera == null)
                _worldCamera = Camera.main;

            if (_worldCamera == null)
            {
                if (_label.gameObject.activeSelf) _label.gameObject.SetActive(false);
                return;
            }

            Vector3 a = _overlay.A;
            Vector3 b = _overlay.B;
            Vector3 mid = (a + b) * 0.5f;

            Vector3 sp = _worldCamera.WorldToScreenPoint(mid);

            // If behind the camera, hide it.
            if (sp.z <= 0.0001f)
            {
                if (_label.gameObject.activeSelf) _label.gameObject.SetActive(false);
                return;
            }

            // Update text
            float d = Vector3.Distance(a, b);
            _label.text = $"{d:0.###} m";

            // Position in screen space
            Vector2 pos = new Vector2(sp.x, sp.y) + _screenOffset;

            if (_clampToScreen)
            {
                float w = Screen.width;
                float h = Screen.height;

                pos.x = Mathf.Clamp(pos.x, _screenPadding.x, w - _screenPadding.x);
                pos.y = Mathf.Clamp(pos.y, _screenPadding.y, h - _screenPadding.y);
            }

            if (!_label.gameObject.activeSelf) _label.gameObject.SetActive(true);

            // IMPORTANT: Screen Space Overlay canvas expects pixel coords here.
            if (_rect != null)
                _rect.position = pos;
            else
                transform.position = pos;
        }
    }
}
