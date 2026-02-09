namespace FuzzPhyte.Placement.Interaction
{
    using System.Collections.Generic;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class PlacementObjectComponent : MonoBehaviour, IFPPlacementSocket
    {
        public PlacementObject PlacementData;
        public Transform RootPlacement;
        public bool Locked = false;
        public List<FP_PlacementSide> Sides = new List<FP_PlacementSide>();
        protected FP_PlacementSide bottomSide;
        protected FP_PlacementSide topSide;
        public FP_PlacementSide GetBottomSide => bottomSide;
        public FP_PlacementSide GetTopSide => topSide;
        public FP_PlacementSocketComponent CurrentSocket { get => currentSocket;}
        [SerializeField] protected FP_PlacementSocketComponent currentSocket;
        [SerializeField] protected FP_PlacementSocketComponent previousSocket;
        public FP_PlacementSide GetSide(FPObjectSideType type)
        {
            return Sides.Find(s => s.SideType == type);
        }
        public void Awake()
        {
            if (RootPlacement == null)
            {
                RootPlacement = this.transform;
            }

            foreach (var side in Sides)
            {
                if (side.SideType == FPObjectSideType.Bottom)
                {
                    bottomSide = side;
                }
                if(side.SideType == FPObjectSideType.Top)
                {
                    topSide = side;
                }
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

        public void OnPlacementInSocket(FP_PlacementSocketComponent socket)
        {
            if (currentSocket == null)
            {
                currentSocket = socket;
                if(previousSocket == null)
                {
                    previousSocket = socket;
                }
            }
        }

        public void OnPlacementRemoved(FP_PlacementSocketComponent socket)
        {
            if(currentSocket == socket)
            {
                currentSocket = null;
            }
        }
        public void OnPlacementOutOfBounds(Vector3 pos, Quaternion rot)
        {
            RootPlacement.SetPositionAndRotation(pos, rot);
            //check if we were thrown back into bounds of another socket
            if (previousSocket != null)
            {
                //did we go back to this socket?
                if (previousSocket.OverrideCanAccept(this,pos))
                {
                    Debug.Log($"Manually moving back into a socket situation");
                }
            }
        }
    }
}
