namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using UnityEngine.Events;

    [DisallowMultipleComponent]
    public sealed class FP_ToolbarHitProvider : MonoBehaviour
    {
        [SerializeField] private FP_ToolbarAction _action;
        [Tooltip("Event invoked on selection")]
        public UnityEvent OnSelectionEvent;
        [Tooltip("Delay an action after start some 'x' seconds")]
        [SerializeField] private bool DelayActionOnStart = false;
        [SerializeField] private float DelaySeconds = 1f;
        public UnityEvent OnStartDelayedEvent;
        public void Start()
        {
            if (DelayActionOnStart)
            {
                Invoke(nameof(OnStartDelayedEvent), DelaySeconds);
            }
        }
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
