namespace FuzzPhyte.Placement.Interaction
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    [DisallowMultipleComponent]
    public class PlacementObjectComponent : MonoBehaviour, IFPPlacementSocket, IFPInteractionClicks
    {
        public PlacementObject PlacementData;
        public Transform RootPlacement;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool drawGizmosOnSelectedOnly = true;
        public bool Locked = false;
        public bool Clickable = true;
        public List<FP_PlacementSide> Sides = new List<FP_PlacementSide>();
        protected FP_PlacementSide bottomSide;
        protected FP_PlacementSide topSide;
        protected float heightValue = 1f;
        public float ReturnHeightValue => heightValue;
        public FP_PlacementSide GetBottomSide => bottomSide;
        public FP_PlacementSide GetTopSide => topSide;
        public FP_PlacementSocketComponent CurrentSocket { get => currentSocket;}
        [SerializeField] protected FP_PlacementSocketComponent currentSocket;
        [SerializeField] protected FP_PlacementSocketComponent previousSocket;
        [Space]
        [Header("Unity Events")]
        public UnityEvent OnPickedUpEvent;
        public UnityEvent OnPlacedEvent;
        public UnityEvent OnSocketPlacedEvent;
        public UnityEvent OnSocketRemovedEvent;
        public UnityEvent OnDoubleClickEvent;
        public UnityEvent OnSingleClickEvent;
        public FP_PlacementSide GetSide(FPObjectSideType type)
        {
            return Sides.Find(s => s.SideType == type);
        }
        public virtual void Awake()
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
            if(topSide!=null&& bottomSide != null)
            {
                heightValue = Vector3.Distance(topSide.transform.position, bottomSide.transform.position);
            }
        }
        protected void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            
            if (PlacementData == null) return;

            if (PlacementData.BuildMode == PlacementBuildMode.Stacking)
            {
                DrawStackGizmos(Color.orange);
            }
            else
            {
                DrawLayoutGizmos(Color.orange);
            }
        }
        protected void OnDrawGizmos()
        {
            if (!drawGizmos || drawGizmosOnSelectedOnly) return;
            if (PlacementData == null) return;
            if (PlacementData.BuildMode == PlacementBuildMode.Stacking)
            {
                DrawStackGizmos(Color.cyan);
            }
            else
            {
                DrawLayoutGizmos(Color.cyan);
            }
        }
        protected void DrawStackGizmos(Color gizmoColor)
        {
            Gizmos.color = gizmoColor;
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
        protected void DrawLayoutGizmos(Color gizmoColorMain)
        {
            Gizmos.color = gizmoColorMain;

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
        public virtual void OnPlacementInSocket(FP_PlacementSocketComponent socket, PlacementObjectComponent obj, Transform rootObj)
        {
            if (currentSocket == null)
            {
                currentSocket = socket;
                if(previousSocket == null)
                {
                    previousSocket = socket;
                }
                Debug.Log($"On Placement In Socket");
                OnSocketPlacedEvent?.Invoke();
            }
        }
        public virtual void OnGeneralPlacement(Vector3 pos, Quaternion rot)
        {
            OnPlacedEvent?.Invoke();
            if(currentSocket != null)
            {
                previousSocket = currentSocket;
                currentSocket = null;
            }
        }
        public virtual void OnPlacementRemoved(FP_PlacementSocketComponent socket, PlacementObjectComponent obj,Transform rootObj )
        {
            Debug.Log($"On Placement Removed");
            if (currentSocket == socket)
            {
                currentSocket = null;
                OnSocketRemovedEvent?.Invoke();
            }
        }
        public virtual void OnPlacementOutOfBounds(Vector3 pos, Quaternion rot)
        {
            RootPlacement.SetPositionAndRotation(pos, rot);
            //check if we were thrown back into bounds of another socket
            if (previousSocket != null)
            {
                //did we go back to this socket?
                if (previousSocket.OverrideCanAccept(this,pos, RootPlacement))
                {
                    OnSocketPlacedEvent?.Invoke();
                }
            }
        }
        public virtual void OnPickupStarted(Vector3 pos)
        {
            OnPickedUpEvent?.Invoke();
        }
        public virtual void OnDoubleClickAction()
        {
            OnDoubleClickEvent?.Invoke();
        }
        public virtual void OnSingleClickAction()
        {
            OnSingleClickEvent?.Invoke();
        }
    }
}
