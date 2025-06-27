namespace FuzzPhyte.Placement
{
    using UnityEngine;

    [DisallowMultipleComponent]
    public class PlacementObjectComponent : MonoBehaviour
    {
        public PlacementObject PlacementData;
        private void OnDrawGizmosSelected()
        {
            if (PlacementData == null) return;

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

    }
}
