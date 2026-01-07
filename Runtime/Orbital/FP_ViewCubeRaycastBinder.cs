namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;

    public class FP_ViewCubeRaycastBinder : MonoBehaviour
    {
        [SerializeField] private FP_ViewCubeUIRaycaster _raycaster;
        [SerializeField] private FP_ViewCubeInterface _viewCubeInterface;

        private void OnEnable()
        {
            if (_raycaster != null)
                _raycaster.OnViewCubeHit += HandleViewCubeHit;
        }

        private void OnDisable()
        {
            if (_raycaster != null)
                _raycaster.OnViewCubeHit -= HandleViewCubeHit;
        }

        private void HandleViewCubeHit(FP_ViewCubeHit hit, RaycastHit raycastHit)
        {
            _viewCubeInterface.ApplyHit(hit);
        }
    }
}
