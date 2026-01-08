namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;

    public class FP_ViewCubeRaycastBinder : MonoBehaviour
    {
        [SerializeField] private FP_ViewCubeUIRaycaster _raycaster;
        [SerializeField] private FP_ViewCubeInterface _viewCubeInterface;
        [SerializeField] private FP_ViewHomeUIRaycast _ViewHomeUIRaycast;

        private void OnEnable()
        {
            if (_raycaster != null)
            {
                _raycaster.OnCubeSelect += HandleViewCubeHit;
                _raycaster.OnViewCubeHover += HandleHoverCubeHit;
                _raycaster.OnViewCubeUnHover += HandleUnHoverCubeHit;
            }
            if(_ViewHomeUIRaycast != null)
            {
                _ViewHomeUIRaycast.OnHomeSelect += HandleHomeViewHit;
            }
               
        }

        private void OnDisable()
        {
            if (_raycaster != null)
            {
                _raycaster.OnCubeSelect -= HandleViewCubeHit;
                _raycaster.OnViewCubeHover -= HandleHoverCubeHit;
                _raycaster.OnViewCubeUnHover -= HandleUnHoverCubeHit;
            }    
        }
        private void HandleViewCubeHit(FP_ViewCubeHitProvider hit, RaycastHit raycastHit)
        {
            _viewCubeInterface.ApplyHit(hit.HitType);
        }
        private void HandleHoverCubeHit(FP_ViewCubeHitProvider hit)
        {
            hit.Hover();
        }
        private void HandleUnHoverCubeHit(FP_ViewCubeHitProvider hit)
        {
            hit.UnHover();
        }
        private void HandleHomeViewHit(FP_ViewHomeHitProvider hit,FP_ProjectionMode projection)
        {
            if (projection == FP_ProjectionMode.Orthographic)
            {
                hit.PerspectiveUIActive();
                _viewCubeInterface.CameraProjectionCheck(FP_ProjectionMode.Perspective);
            }
            else
            {
                hit.OrthoUIActive();
                _viewCubeInterface.CameraProjectionCheck(FP_ProjectionMode.Orthographic);
            }
           
        }
    }
}
