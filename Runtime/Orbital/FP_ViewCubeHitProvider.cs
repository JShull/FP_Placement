namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using TMPro;
    [DisallowMultipleComponent]
    public class FP_ViewCubeHitProvider : MonoBehaviour
    {
        [SerializeField] private FP_ViewCubeHit _hitType;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Material _hoverMaterial;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _selectedMaterial;
        [SerializeField] private bool _selectionActive;

        public string LabelName;
        public FP_ViewCubeHit HitType => _hitType;
        public Transform LabelLocation;
        public GameObject TextLabelPrefab;
        private GameObject _labelRef;
        private TextMeshPro _textRef;

        public void Awake()
        {
            if (TextLabelPrefab != null)
            {
                var spawnLocation = LabelLocation != null ? LabelLocation : this.transform;
                _labelRef=GameObject.Instantiate(TextLabelPrefab, spawnLocation);
                _labelRef.name = _hitType.ToString() + "_" + LabelName;
                _textRef = _labelRef.GetComponent<TextMeshPro>();
                if (_textRef!=null && LabelName.Length>0)
                {
                    _textRef.text = LabelName;
                }
                else if(_textRef!=null)
                {
                    _textRef.text = "";
                }
            }
        }
        public void Hover()
        {
            if (!_selectionActive)
            {
                _meshRenderer.material = _hoverMaterial;
            }
        }
        public void UnHover()
        {
            if (!_selectionActive)
            {
                _meshRenderer.material = _normalMaterial;
            }
        }
        public void Select()
        {
            _selectionActive = true;
            _meshRenderer.material = _selectedMaterial;
            
        }
        public void UnSelect()
        {
            _selectionActive = false;
            UnHover();
        }
    }
}
