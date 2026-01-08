namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    [DisallowMultipleComponent]
    public sealed class FP_ToolbarHitProvider : MonoBehaviour
    {
        [SerializeField] private FP_ToolbarAction _action;

        public FP_ToolbarAction Action => _action;

        // Optional: hover/selection visual hooks like your cube provider
        public void Hover() { }
        public void UnHover() { }
        public void Select() { }
        public void UnSelect() { }
    }
}
