namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using TMPro;
    using FuzzPhyte.Utility;
    using Codice.Client.BaseCommands;

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

        [SerializeField] private RectTransform canvasRect;

        private void Awake()
        {
            //_rect = transform as RectTransform;
            if (_label == null)
            {
                Debug.LogError("Missing a label!");
                return;
            }
                
            if (canvasRect == null)
                canvasRect = _label.canvas.transform as RectTransform;
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
            // update units
            (bool gotValue, float distance) = FP_UtilityData.ConvertValue(d, UnitOfMeasure.Meter, _overlay.Units);
            if (gotValue)
            {
                var unitAbb = FP_UtilityData.GetUnitAbbreviation(_overlay.Units);
                _label.text = $"{distance:0.##} {unitAbb}";
            }
            else
            {
                _label.text = $"{d:0.###} m";
            }

            // Position in screen space
            Vector2 pos = new Vector2(sp.x, sp.y) + _screenOffset;
            
            if (_clampToScreen)
            {
                // Clamp in CANVAS space (not Screen.width/height)
                Rect r = canvasRect.rect;
                pos.x = Mathf.Clamp(pos.x, r.xMin + _screenPadding.x, r.xMax - _screenPadding.x);
                pos.y = Mathf.Clamp(pos.y, r.yMin + _screenPadding.y, r.yMax - _screenPadding.y);
            }

            if (!_label.gameObject.activeSelf) _label.gameObject.SetActive(true);

            //RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,pos,null,out Vector2 localPoint);
            canvasRect.anchoredPosition = pos;

        }
    }
}
