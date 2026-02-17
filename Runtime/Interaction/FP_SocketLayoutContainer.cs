namespace FuzzPhyte.Placement.Interaction
{
    using UnityEngine;
    using System.Collections.Generic;

    [RequireComponent(typeof(FP_PlacementSocketComponent))]
    public class FP_SocketLayoutContainer : MonoBehaviour
    {
        [Header("Layout Source")]
        public bool BuildOnStart = false;
        [Tooltip("This will use the Quad Area Placer to layout the slots")]
        public bool UseQuadPlacementOnSetup = false;
        public bool TurnOffSlotRenderers = true;
        [SerializeField] private FP_PlacementSocketComponent socket;
        [SerializeField] private Transform quadSource;
        [Range(4,32)]
        public int CircularLayout =24;
        public int OptimizationPasses = 5000;
        [Range(0.01f,.5f)]
        public float ItemPadding = 0.05f;
        public QuadAreaPlacer.QuadStartAnchor StartAnchor = QuadAreaPlacer.QuadStartAnchor.Center;
        public QuadAreaPlacer.PlacementSortMode SortMode = QuadAreaPlacer.PlacementSortMode.SmallestFirst;
        public QuadAreaPlacer.QuadSizeMode SizeMode = QuadAreaPlacer.QuadSizeMode.TransformScaleOnly;
        [Header("Runtime Slots")]
        [SerializeField] private List<Transform> slotPoints = new List<Transform>();

        // Slot occupancy
        private bool[] filled;

        // Track which placement object is in which slot
        private Dictionary<PlacementObjectComponent, int> placementToSlot =
            new Dictionary<PlacementObjectComponent, int>();

        

        private void Awake()
        {
            if (socket == null)
            {
                socket = GetComponent<FP_PlacementSocketComponent>();
            }
        }
        private void OnEnable()
        {
            if (socket != null)
            {
                socket.OnPlacementAddedAction+=OnPlacementAdded;
                socket.OnPlacementRemovedAction+=OnPlacementRemoved;
            }
        }
        private void OnDisable()
        {
            if (socket != null)
            {
                socket.OnPlacementAddedAction-=OnPlacementAdded;
                socket.OnPlacementRemovedAction-=OnPlacementRemoved;
            } 
        }
        [ContextMenu("Layout Slots")]
        public void LayoutInEditor()
        {
            BuildSlotsFromChildren();
        }
        private void Start()
        {
            if (BuildOnStart)
            {
                BuildSlotsFromChildren();
            }
        }

        /// <summary>
        /// Slot Setup
        /// </summary>
        private void BuildSlotsFromChildren()
        {
            filled = new bool[slotPoints.Count];
            if(socket==null || quadSource == null)
            {
                Debug.LogError("[SocketLayout] Missing socket or quad source reference.");
                return;
            }
            // Update socket capacity
            socket.Capacity = slotPoints.Count;
            // use layout Quad Area
            if (UseQuadPlacementOnSetup)
            {
                QuadAreaPlacer.ApplyToQuadArea(
                slotPoints,
                quadSource,
                false,
                0,
                0.85f,
                ItemPadding,
                0.1f,
                OptimizationPasses,
                5,CircularLayout,StartAnchor,SortMode, SizeMode);
            Debug.Log($"[SocketLayout] Built {slotPoints.Count} slots.");
            }
            if (TurnOffSlotRenderers)
            {
                foreach (var t in slotPoints)
                {
                    var renderers = t.GetComponentsInChildren<Renderer>();
                    foreach (var r in renderers)
                    {
                        r.enabled = false;
                    }
                }
            } 
        }
        /// <summary>
        /// Check Slots Availability
        /// </summary>
        /// <returns></returns>
        public bool HasOpenSlot()
        {
            for (int i = 0; i < filled.Length; i++)
            {
                if (!filled[i])
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Just gets the next open slot index
        /// </summary>
        /// <returns></returns>
        private int GetNextOpenSlot()
        {
            for (int i = 0; i < filled.Length; i++)
            {
                if (!filled[i])
                    return i;
            }

            return -1;
        }
        public void OnPlacementAdded(PlacementObjectComponent item)
        {
            Debug.Log($"[SocketLayout] Attempting to place {item.PlacementData.name} into slot.");
            TryPlaceIntoSlot(item,item.RootPlacement);
        }
        public void OnPlacementRemoved(PlacementObjectComponent item)
        {
            Debug.Log($"[SocketLayout] Removing {item.PlacementData.name} from slot.");
            RemovePlacement(item);
        }
        // -----------------------------
        // Placement Handling
        // -----------------------------
        private bool TryPlaceIntoSlot(
            PlacementObjectComponent placement,
            Transform placementTransform)
        {
            if (!HasOpenSlot())
            {
                Debug.Log("[SocketLayout] No open slots available.");
                return false;
            }

            int slotIndex = GetNextOpenSlot();
            Transform slot = slotPoints[slotIndex];

            // Move object into slot position
            placementTransform.SetPositionAndRotation(
                slot.position,
                slot.rotation
            );

            // Mark slot filled
            filled[slotIndex] = true;
            placementToSlot[placement] = slotIndex;

            // Update socket state - should already happen via the occupants that are there.
            //socket.CurrentCount++;

            Debug.Log($"[SocketLayout] Placed {placement.name} into slot {slotIndex}");
            return true;
        }

        // -----------------------------
        // Removal Handling
        // -----------------------------
        private void RemovePlacement(PlacementObjectComponent placement)
        {
            if (!placementToSlot.ContainsKey(placement))
                return;

            int slotIndex = placementToSlot[placement];

            filled[slotIndex] = false;
            placementToSlot.Remove(placement);

            //socket.CurrentCount--;

            Debug.Log($"[SocketLayout] Removed {placement.name} from slot {slotIndex}");
        }
    }
}
