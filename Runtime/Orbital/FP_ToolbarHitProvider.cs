namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using UnityEngine.Events;

    [System.Serializable]
    public class FPToolbarProviderEvent : UnityEvent<FP_ToolbarHitProvider> { }
    
    [DisallowMultipleComponent]
    public sealed class FP_ToolbarHitProvider : MonoBehaviour
    {
        [SerializeField] private FP_ToolbarAction _action;
        [Tooltip("Event invoked on selection")]
        public UnityEvent OnSelectionEvent;
        [Tooltip("Delay an action after start some 'x' seconds")]
        [SerializeField] private bool DelayActionOnStart = false;
        [SerializeField] private float DelaySeconds = 1f;
        [Tooltip("Invoked after delay and passes this provider dynamically")]
        public FPToolbarProviderEvent OnStartDelayedEvent;
        public FP_ToolbarAction Action => _action;
        public void Start()
        {
            if (DelayActionOnStart)
            {
                Invoke(nameof(InvokeStartDelayed), DelaySeconds);
            }
        }
        

        // Optional: hover/selection visual hooks like your cube provider
        public void Hover() { }
        public void UnHover() { }
        public void Select() 
        {
            OnSelectionEvent?.Invoke();
        }
        public void UnSelect() { }
        private void InvokeStartDelayed()
        {
            OnStartDelayedEvent?.Invoke(this);
        }
    }
}
