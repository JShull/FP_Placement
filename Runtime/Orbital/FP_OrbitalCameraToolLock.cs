namespace FuzzPhyte.Placement.OrbitalCamera
{
    using UnityEngine;

    public sealed class FP_OrbitalCameraToolLock : MonoBehaviour
    {
        [SerializeField] private FP_OrbitalMouseInputBehaviour _input; // can later generalize to other adapters
        [SerializeField] private FP_OrbitalCameraBehaviour _orbital;

        public Camera Camera => _orbital != null ? _orbital.GetComponentInChildren<Camera>() : null;

        public bool IsOrthographic => Camera != null && Camera.orthographic;

        public void SetToolInputLocked(bool locked)
        {
            if (_input != null) _input.SetInputLocked(locked); // already handles force release :contentReference[oaicite:3]{index=3}
        }
    }
}
