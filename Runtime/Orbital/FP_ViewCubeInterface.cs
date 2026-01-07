namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;
    public class FP_ViewCubeInterface : MonoBehaviour
    {
        [Header("Orbital Camera")]
        [SerializeField] private FP_OrbitalCameraBehaviour _orbital;

        [Header("Target Frame (defines Front/Top/Right)")]
        [Tooltip("If null, uses world axes.")]
        [SerializeField] private Transform _targetFrame;

        [Header("View Cube Visual")]
        [SerializeField] private Transform _viewCubeTransform;

        [Header("Behaviour")]
        [SerializeField] private FP_ProjectionMode _snapProjection = FP_ProjectionMode.Orthographic;

        private void OnEnable()
        {
            if (_orbital != null)
            {
                _orbital.OnCameraRotationApplied += HandleCameraRotationApplied;
            }
        }
        private void OnDisable()
        {
            if (_orbital != null)
            {
                _orbital.OnCameraRotationApplied -= HandleCameraRotationApplied;
            }
        }
        private void HandleCameraRotationApplied(FP_OrbitalViewState orbitalViewState)
        {
            SyncViewCube(orbitalViewState.Rotation);
        }
        // Call this from your raycast system when the user hovers/clicks a face/corner.
        public void ApplyHit(FP_ViewCubeHit hit)
        {
            if (_orbital == null) return;

            // 1) Convert hit -> pose in target-frame local axes
            FP_ViewPose pose = FP_ViewCubePoses.Get(hit);
            pose.NormalizeDirection();

            // 2) Convert pose to world space using target frame
            Quaternion targetRotation = BuildWorldRotation(pose, _targetFrame);

            // 3) Drive orbital camera
            // Option A (best): add a method on the controller to set rotation target directly.
            // Option B (ok): map to your existing SnapView for faces only.
            // Here’s Option A:
            SetOrbitalRotationTarget(targetRotation);

            // 4) Rotate the view cube visual to match the camera/view
            // Depending on how your cube is modeled, you may want inverse rotation.
            SyncViewCube(targetRotation);
        }

        private static Quaternion BuildWorldRotation(FP_ViewPose pose, Transform frame)
        {
            // Convert “FromDirection” into a camera rotation that LOOKS toward pivot:
            // camera forward should be opposite of FromDirection
            Vector3 from = pose.FromDirection;
            Vector3 up = pose.UpDirection;

            if (frame != null)
            {
                from = frame.TransformDirection(from);
                up = frame.TransformDirection(up);
            }

            Vector3 forward = -from.normalized;
            up = up.normalized;

            return Quaternion.LookRotation(forward, up);
        }

        private void SetOrbitalRotationTarget(Quaternion worldRotation)
        {
            // Minimal, practical approach:
            // - expose a method on FP_OrbitalCameraBehaviour (or controller) like:
            //   Controller.SetRotationTarget(worldRotation, projectionMode)
            //
            // Since your current controller shown earlier targets _rotationTarget internally,
            // you'd add a public method there.
            _orbital.Controller.SetRotationTarget(worldRotation, _snapProjection,false);
        }

        private void SyncViewCube(Quaternion cameraRotation)
        {
            if (_viewCubeTransform == null) return;

            // Typical view cube rotates WITH the camera orientation = cameraRotation
            // _viewCubeTransform.rotation = cameraRotation;
            // If we want to mirror the item in the world swap to Quaternion.Inverse(cameraRotation).
            _viewCubeTransform.rotation = Quaternion.Inverse(cameraRotation);
        }
    }
}
