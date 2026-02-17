namespace FuzzPhyte.Placement
{
    using UnityEngine;

    public class FP_PlacementSide : MonoBehaviour
    {
        public FPObjectSideType SideType;
        [SerializeField]private bool drawGizmos = false;
        [Header("Quad Gizmo Settings")]
        public Vector2 SurfaceSize = new Vector2(1f, 1f);
        public Vector3 Normal = Vector3.up;

        private void OnDrawGizmos()
        {
            if(!drawGizmos) return;
            Gizmos.color = Color.cyan;

            Vector3 center = transform.position;

            // Normal is always transform.up
            //Vector3 normal = transform.up;

            // Build plane axes perpendicular to normal
            Vector3 CrossVector = transform.right;
            if(SideType==FPObjectSideType.Right || SideType == FPObjectSideType.Left)
            {
                CrossVector = transform.forward;
            }
            Vector3 axisA = Vector3.Cross(Normal, CrossVector).normalized;
            Vector3 axisB = Vector3.Cross(Normal, axisA).normalized;

            // Quad half extents
            Vector3 halfA = axisA * (SurfaceSize.x * 0.5f);
            Vector3 halfB = axisB * (SurfaceSize.y * 0.5f);

            // Compute quad corners
            Vector3 p1 = center - halfA - halfB;
            Vector3 p2 = center + halfA - halfB;
            Vector3 p3 = center + halfA + halfB;
            Vector3 p4 = center - halfA + halfB;

            // Draw quad outline
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);

            // Draw normal direction
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(center, center + Normal * 0.5f);
        }

    }
}
