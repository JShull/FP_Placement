namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using UnityEngine.Events;

    [DisallowMultipleComponent]
    public sealed class FP_ToolbarHitProvider : MonoBehaviour
    {
        [SerializeField] private FP_ToolbarAction _action;
        public UnityEvent OnSelectionEvent;

        public FP_ToolbarAction Action => _action;

        // Optional: hover/selection visual hooks like your cube provider
        public void Hover() { }
        public void UnHover() { }
        public void Select() 
        {
            OnSelectionEvent?.Invoke();
        }
        public void UnSelect() { }
    }
}
