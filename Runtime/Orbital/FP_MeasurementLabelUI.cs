namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using TMPro;
    using FuzzPhyte.Utility;
    using System;

    /// <summary>
    /// Always-on-top measurement label (Screen Space Overlay).
    /// Reads A/B from FPRuntimeMeasurementOverlay and positions a TMPUGUI label
    /// at the midpoint projected into screen space.
    /// </summary>
    public sealed class FP_MeasurementLabelUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private FPRuntimeMeasurementOverlay _overlay;
        [SerializeField] private TMP_Text _label; // TextMeshProUGUI or TMP_Text
        [SerializeField] private RectTransform _labelRect;

        [Header("Layout")]
        [Tooltip("Extra pixel offset applied after projecting midpoint into screen space.")]
        [SerializeField] private Vector2 _screenOffset = new Vector2(0f, 18f);

        [Tooltip("Clamp label to screen bounds (keeps it visible even at edges).")]
        [SerializeField] private bool _clampToScreen = true;

        [Tooltip("Padding (pixels) used when clamping to screen.")]
        [SerializeField] private Vector2 _screenPadding = new Vector2(8f, 8f);
        
        [Space]
        [Header("World Canvas Parameters")]
        [SerializeField] private bool _useWorldLabel = false;
        [SerializeField] private bool _faceCamera = true;
        [Tooltip("World-space TMP label (e.g., TextMeshPro 3D).")]
        [SerializeField] private TMP_Text _worldLabel;

        [Tooltip("Distance from camera along camera->midpoint ray. This makes it feel 'on top' of geometry.")]
        [SerializeField] private float _towardCameraFromMid = 0.15f;
        [Tooltip("Distance from the midpoint vertical up on world Up")]
        [SerializeField] private float _worldUpFromMid = 0.15f;

        [Tooltip("Extra offset in world space after placing along the ray (uses camera up/right).")]
        [SerializeField] private Vector2 _worldLabelScreenLikeOffset = new Vector2(0f, 0.08f);

        [Tooltip("If true, pushes label forward to just in front of the first hit toward midpoint.")]
        [SerializeField] private bool _useOcclusionPush = true;

        [Tooltip("Layers considered solid for occlusion push.")]
        [SerializeField] private LayerMask _occlusionMask = ~0;

        [Tooltip("How far in front of the occluder the label should sit.")]
        [SerializeField] private float _occlusionEpsilon = 0.02f;

        private Vector3 _lastA;
        private Vector3 _lastB;
        private UnitOfMeasure _lastUnit;
        private bool _hadMeasureLastFrame;
        private string _cachedText = string.Empty;
        private float _cachedDistanceInUnits = 0f;
        private float _valueEpsilonMeters = 0.0005f;
        #region Actions - Events
        public event Action<TMP_Text,float, UnitOfMeasure> MeasurementChanged;
        #endregion

        private void Awake()
        {
            //_rect = transform as RectTransform;
            if (_label == null && _worldLabel==null)
            {
                Debug.LogError("Missing a overlay and/or world label - need at least one!");
                return;
            }
                
            if (canvasRect == null)
                canvasRect = _label.canvas.transform as RectTransform;
            if (_overlay == null)
                _overlay = FPRuntimeMeasurementOverlay.Active;

            if (_worldCamera == null)
                _worldCamera = Camera.main;
            if (_useWorldLabel && _worldLabel == null)
            {
                _useWorldLabel = false;
                Debug.LogError($"Can't use world label without a world label!");
            }
        }

        private void LateUpdate()
        {
            if (_overlay == null)
                _overlay = FPRuntimeMeasurementOverlay.Active;

            bool hasMeasure = (_overlay != null && _overlay.HasMeasurement);

            if (!hasMeasure || _worldCamera == null)
            {
                SetActiveSafe(_label, false);
                SetActiveSafe(_worldLabel, false);
                _hadMeasureLastFrame = false;
                _cachedText = string.Empty;
                return;
            }

            Vector3 a = _overlay.A;
            Vector3 b = _overlay.B;
            Vector3 mid = (a + b) * 0.5f;

            Vector3 sp = _worldCamera.WorldToScreenPoint(mid);
            // text first
            // Prepare the text once OLD
            /*
            float dMeters = Vector3.Distance(a, b);

            (bool gotValue, float distance) = FP_UtilityData.ConvertValue(dMeters, UnitOfMeasure.Meter, _overlay.Units);
            string text;
            if (gotValue)
            {
                var unitAbb = FP_UtilityData.GetUnitAbbreviation(_overlay.Units);
                text = $"{distance:0.##} {unitAbb}";
            }
            else
            {
                text = $"{dMeters:0.###} m";
            }
            */
            RefreshMeasurementTextIfNeeded(a, b, _overlay.Units);
            if (_useWorldLabel)
            {
                // WORLD LABEL MODE
                if (_worldLabel == null)
                {
                    // If user toggles world mode but didn't assign a world label
                    SetActiveSafe(_label, false);
                   
                    return;
                }

                //_worldLabel.text = text;

                Vector3 camPos = _worldCamera.transform.position;

                // lifted midpoint
                Vector3 liftedMid = mid + (Vector3.up * _worldUpFromMid);
                
                // Direction from liftedMid toward camera
                Vector3 toCam = (camPos - liftedMid);
                float toCamMag = toCam.magnitude;
                if (toCamMag <= 0.0001f) return;

                Vector3 toCamDir = toCam / toCamMag;

                // MIDPOINT-FIRST: place near midpoint, then pull slightly toward camera
                Vector3 worldPos = liftedMid + toCamDir * _towardCameraFromMid;


                float camToWorld = Vector3.Distance(camPos, worldPos);
                if (camToWorld < 0.05f)
                {
                    worldPos = camPos + (worldPos - camPos).normalized * 0.05f;
                }


                // Base placement: fixed distance from camera along the ray
                if (_useOcclusionPush)
                {
                    Vector3 camToLabel = worldPos - camPos;
                    float camToLabelDist = camToLabel.magnitude;

                    if (camToLabelDist > 0.001f)
                    {
                        Vector3 camToLabelDir = camToLabel / camToLabelDist;
                        if (Physics.Raycast(camPos, camToLabelDir, out RaycastHit hit, camToLabelDist, _occlusionMask, QueryTriggerInteraction.Ignore))
                        {
                            worldPos = camPos + camToLabelDir * Mathf.Max(0.05f, hit.distance - _occlusionEpsilon);
                        }
                    }
                }


                // Apply a small "screen-like" offset using camera basis
                worldPos += _worldCamera.transform.right * _worldLabelScreenLikeOffset.x;
                worldPos += _worldCamera.transform.up * _worldLabelScreenLikeOffset.y;

                Transform t = _worldLabel.transform;
                t.position = worldPos;

                // Face the camera (billboard). Want Fixed in world orientation? remove this.
                if (_faceCamera)
                {
                    t.rotation = Quaternion.LookRotation(t.position - camPos, _worldCamera.transform.up);
                }

                SetActiveSafe(_worldLabel, true);
                SetActiveSafe(_label, false);
                return;
            }

            if (_label == null || _labelRect == null || canvasRect == null)
            {
                SetActiveSafe(_label, false);
                SetActiveSafe(_worldLabel, false);
                _hadMeasureLastFrame = false;
                _cachedText = string.Empty;
                return;
            }
           // _label.text = text;


            // If behind camera, hide
            if (sp.z <= 0.0001f)
            {
                SetActiveSafe(_label, false);
                _hadMeasureLastFrame = false;
                _cachedText = string.Empty;
                SetActiveSafe(_worldLabel, false);
                return;
            }

            Vector2 pos = new Vector2(sp.x, sp.y) + _screenOffset;

            if (_clampToScreen)
            {
                pos.x = Mathf.Clamp(pos.x, _screenPadding.x, Screen.width - _screenPadding.x);
                pos.y = Mathf.Clamp(pos.y, _screenPadding.y, Screen.height - _screenPadding.y);
            }

            SetActiveSafe(_label, true);
            SetActiveSafe(_worldLabel, false);

            // move the LABEL rect
            _labelRect.position = pos;
        }
        
        private void RefreshMeasurementTextIfNeeded(Vector3 a, Vector3 b, UnitOfMeasure units)
        {
            bool unitsChanged = units != _lastUnit;

            // Only recompute value if endpoints (or units) changed meaningfully
            float prevMeters = _hadMeasureLastFrame ? Vector3.Distance(_lastA, _lastB) : -1f;
            float currMeters = Vector3.Distance(a, b);

            bool valueChanged =
                !_hadMeasureLastFrame ||
                unitsChanged ||
                Mathf.Abs(currMeters - prevMeters) > _valueEpsilonMeters;

            if (!valueChanged) return;

            (bool gotValue, float distance) = FP_UtilityData.ConvertValue(currMeters, UnitOfMeasure.Meter, units);
            string text;

            if (gotValue)
            {
                var unitAbb = FP_UtilityData.GetUnitAbbreviation(units);
                text = $"{distance:0.##} {unitAbb}";
                _cachedDistanceInUnits = distance;
            }
            else
            {
                text = $"{currMeters:0.###} m";
                _cachedDistanceInUnits = currMeters;
            }

            _cachedText = text;

            // Update whichever label(s) exist
            if (_label != null) _label.text = _cachedText;
            if (_worldLabel != null) _worldLabel.text = _cachedText;
            _lastA = a;
            _lastB = b;
            _lastUnit = units;
            MeasurementChanged?.Invoke(_useWorldLabel ? _worldLabel : _label, _cachedDistanceInUnits, units);
           
            _hadMeasureLastFrame = true;
        }
        private static void SetActiveSafe(TMP_Text t, bool on)
        {
            if (t == null) return;
            if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }
        #region Public API Methods
        public void UpdateOffsetDetails(float cameraFromMid, float worldupFromMid, UnitOfMeasure newUnits)
        {
            _towardCameraFromMid = cameraFromMid;
            _worldUpFromMid = worldupFromMid;
            if (_worldLabel != null)
            {
                switch (newUnits)
                {
                    case UnitOfMeasure.Millimeter:
                        _worldLabel.rectTransform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                        break;
                    case UnitOfMeasure.Centimeter:
                        _worldLabel.rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                        break;
                    case UnitOfMeasure.Meter:
                        _worldLabel.rectTransform.localScale = new Vector3(0.1f, 0.1f,0.1f);
                        break;
                    case UnitOfMeasure.Inch:
                        _worldLabel.rectTransform.localScale = new Vector3(0.0254f, 0.0254f, 0.0254f);
                        break;
                }
            }
            
        }
        #endregion
    }
}
