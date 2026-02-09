namespace FuzzPhyte.Placement.Interaction
{
    using UnityEngine;

    public class FP_SocketVisual : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Material normalMat;
        [SerializeField] private Material hoverMat;
        [SerializeField] private Material selectMat;
        [SerializeField] private Material capacityMat;

        public void OnHoverEnter(FP_PlacementSocketComponent socket)
        {
            if (targetRenderer == null) return;
            targetRenderer.material = hoverMat;
        }

        public void OnHoverExit(FP_PlacementSocketComponent socket)
        {
            if (targetRenderer == null) return;
            targetRenderer.material = normalMat;
        }
        public void OnEndSelected(FP_PlacementSocketComponent socket)
        {
            if (targetRenderer == null) return;
            targetRenderer.material = selectMat;
        }
        
        public void OnCapacityFull(FP_PlacementSocketComponent socket)
        {
            if(targetRenderer== null) return;
            targetRenderer.material = capacityMat;
        }
    }
}
