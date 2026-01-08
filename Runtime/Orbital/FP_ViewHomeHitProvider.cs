namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    using UnityEngine.Events;

    public class FP_ViewHomeHitProvider : MonoBehaviour
    {
        [SerializeField] private FP_ViewHomeHit _hitType;
        public UnityEvent PerspectiveActive;
        public UnityEvent OrthoActive;

        public FP_ViewHomeHit HitType => _hitType;

        public void PerspectiveUIActive()
        {
            PerspectiveActive.Invoke();
        }
        public void OrthoUIActive()
        {
            OrthoActive.Invoke();
        }
    }
}
