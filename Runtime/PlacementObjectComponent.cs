namespace FuzzPhyte.Placement.Interaction
{
    using UnityEngine;

    [DisallowMultipleComponent]
    public class PlacementObjectComponent : MonoBehaviour
    {
        public PlacementObject PlacementData;
        public Transform RootPlacement;
        public FP_PlacementSocketComponent CurrentSocket { get; set; }
        public void Awake()
        {
            if (RootPlacement == null)
            {
                RootPlacement = this.transform;
            }
        }
        private void OnDrawGizmosSelected()
        {
            if (PlacementData == null) return;

            if (PlacementData.BuildMode == PlacementBuildMode.Stacking)
            {
                DrawStackGizmos();
            }
            else
            {
                DrawLayoutGizmos();
            }

            
        }
        private void DrawStackGizmos()
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + PlacementData.StackCenterOffset;

            switch (PlacementData.Shape)
            {
                case ShapeType.Circle:
#if UNITY_EDITOR
                    UnityEditor.Handles.DrawWireDisc(center, Vector3.up, PlacementData.StackSize.x);
#endif
                    break;
                case ShapeType.Ellipse:
#if UNITY_EDITOR
                    UnityEditor.Handles.DrawWireDisc(center, Vector3.up, (PlacementData.StackSize.x + PlacementData.StackSize.y) * 0.5f);
#endif
                    break;
                case ShapeType.Rectangle:
                    Gizmos.DrawWireCube(center, new Vector3(PlacementData.StackSize.x, 0.01f, PlacementData.StackSize.y));
                    break;
            }
        }
        private void DrawLayoutGizmos()
        {
            Gizmos.color = Color.cyan;

            if (PlacementData.LayoutSurface == LayoutSurfaceType.SphereSurface)
            {
                Vector3 center = transform.position;
                Gizmos.DrawWireSphere(center, PlacementData.SphereRadius);
            }
            else
            {
                Vector3 center = transform.position + PlacementData.BoxCenterOffset;
                Gizmos.DrawWireCube(center, PlacementData.BoxSize);
            }
        }
    }
}
