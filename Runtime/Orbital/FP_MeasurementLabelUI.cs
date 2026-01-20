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
                return;
            }

            Vector3 a = _overlay.A;
            Vector3 b = _overlay.B;
            Vector3 mid = (a + b) * 0.5f;

            Vector3 sp = _worldCamera.WorldToScreenPoint(mid);
            // text first
            // Prepare the text once
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
            if (_useWorldLabel)
            {
                // WORLD LABEL MODE
                if (_worldLabel == null)
                {
                    // If user toggles world mode but didn't assign a world label
                    SetActiveSafe(_label, false);
                    return;
                }

                _worldLabel.text = text;

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
                return;
            }
            _label.text = text;


            // If behind camera, hide
            if (sp.z <= 0.0001f)
            {
                SetActiveSafe(_label, false);
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
        private static void SetActiveSafe(TMP_Text t, bool on)
        {
            if (t == null) return;
            if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }
    }
}
