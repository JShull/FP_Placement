namespace FuzzPhyte.Placement.Interaction
{
    using UnityEngine;
    using System;
    using UnityEngine.Events;
    [Serializable] public class PlacementInteractionEvent : UnityEvent<PlacementObjectComponent,FP_PlacementSocketComponent> { }
    public class FP_PlacementInteractionBehaviour : PlacementBaseInput
    {
        [Space]
        [Header("Placement")]
        [SerializeField] protected bool drawDebug = false;
        [SerializeField] private float maxRayDistance = 100f;
        [SerializeField] private PlacementObject _activePlacement;
        [SerializeField] private PlacementObjectComponent _activeComponent;
        [SerializeField] private PlacementObjectComponent _clickedComponent;
        [SerializeField] private FP_PlacementSocketComponent _activeSocket;
        [SerializeField] private Vector3 _startPos;
        [SerializeField] private Quaternion _startRot;
        
        [Space]
        [Header("Drag Surface Parameters")]
        [SerializeField] private LayerMask surfaceMask;
        [Tooltip("Make sure this is large enough if you decide to stack objects vertically, consider dynamically changing this based on some true ground location")]
        [SerializeField] private float surfaceCastDistance = 10f;
        [Tooltip("Not needed, but if we have it we will update our Surface Cast Distance to make sure we can reach it vertically")]
        [SerializeField] private Transform lowestGroundPointRef;
        [SerializeField] private Transform _currentSurface;
        [SerializeField] private Plane _currentSurfacePlane;
        [Tooltip("We will use the height of the object were dragging * this offset to adjust vertical placement of box cast")]
        [SerializeField] private float surfaceCastVertialOffsetScalar = 1.1f;
        [Header("Private Parameters")]
        [SerializeField] private float _dragRayDistance;
        [SerializeField] private Vector3 _dragLocalOffset;
        [SerializeField] private Transform _dragTarget;
        
        [Space]
        [Header("Socket Hover Parameters")]
        [SerializeField] private FP_PlacementSocketComponent _hoverSocket;
        [SerializeField] private FP_PlacementSocketComponent _previousHoverSocket;
        [SerializeField] private FP_PlacementSocketComponent newHover;
        
        [Space]
        [Header("Magnet Parameters")]
        [SerializeField] private bool useSocketMagnet = true;
        [SerializeField] private float magnetRange = 0.5f;
        [SerializeField] private float magnetStrength =8f;
        [SerializeField] private float magnetSnapDistance =0.05f;
        
        [Space]
        [Header("Placement Bounds")]
        [SerializeField] private bool enforceBounds = true;
        [SerializeField] private Vector3 boundsCenter = Vector3.zero;
        [SerializeField] private Vector3 boundsSize = new Vector3(10f, 5f, 10f);
        public FuzzPhyte.Utility.FP_UtilityDraw fpDrawer;
        [Space]
        [Header("Perspective Placement Assist")]
        [SerializeField] private bool useCursorSurfaceAssist = true;
        [SerializeField] private LayerMask perspectiveSurface;
        [SerializeField] private float cursorAssistMaxDistance = 50f;
        private Vector3 _cursorXZAnchor;
        private bool _hasCursorAnchor;

        [Header("Clicky Placement Events")]
        [SerializeField] public PlacementInteractionEvent doubleClickEvent;
        [SerializeField] public PlacementInteractionEvent singleClickEvent;
        [SerializeField] public PlacementInteractionEvent dragEndSocketSuccessEvent;
        [SerializeField] public PlacementInteractionEvent dragEndSocketFailedEvent;
        [SerializeField] public PlacementInteractionEvent dragEndMovedLocationEvent;
        public override void OnEnable()
        {
            base.OnEnable();
        }
        public override void OnDisable()
        {
            base.OnDisable();
        }
        protected override void UpdateLogic()
        {
            if(_state !=InputState.Dragging) return;
            if (_activePlacement == null) return;
            Ray ray = targetCamera.ScreenPointToRay(_pointerPosition.action.ReadValue<Vector2>());
            UpdateDrag(ray);
            
            
        }
        #region Drag Related Logic
        protected override void OnDragStarted()
        {
            if(_state != InputState.Dragging)
            {
                return;
            }
            Ray ray = targetCamera.ScreenPointToRay(_pointerPosition.action.ReadValue<Vector2>());
            var allHits = Physics.RaycastAll(ray,maxRayDistance,placementMask);
            if (allHits == null || allHits.Length == 0)return;
            for(int i=0;i<allHits.Length;i++)
            {
                //only need to hit something with PlacementObjectComponent on it
                var hit = allHits[i];
                if (drawDebug)
                {
                    Debug.DrawRay(ray.origin, ray.direction*maxRayDistance, Color.red, 2f);
                }
                
                if (hit.collider.TryGetComponent(out PlacementObjectComponent poc))
                {
                    if (poc.Locked) continue;

                    _activeComponent = poc;
                    _activePlacement = poc.PlacementData;
                    _startPos = poc.RootPlacement.position;
                    _startRot = poc.RootPlacement.rotation;
                    _dragTarget = poc.RootPlacement;
                    if(poc.gameObject.TryGetComponent(out IFPPlacementSocket placementInt))
                    {
                        placementInt.OnPickupStarted(_startPos);
                    }
                    if (poc.CurrentSocket != null)
                    {
                        //remove interface details
                        if(_activeComponent.CurrentSocket.gameObject.TryGetComponent(out IFPPlacementSocket socketInt))
                        {
                            socketInt.OnPlacementRemoved(_activeComponent.CurrentSocket,poc,poc.RootPlacement);
                        }
                    }
                    // plane work
                    // Where did we hit the object?
                    Plane dragPlane = new Plane(-ray.direction, _dragTarget.position);
                    dragPlane.Raycast(ray, out _dragRayDistance);

                    // Offset so we don’t snap pivot to cursor
                    Vector3 hitPoint = ray.GetPoint(_dragRayDistance);
                    _dragLocalOffset = _dragTarget.position - hitPoint;
                    break;
                }
            }
        }
       
        protected void UpdateDrag(Ray ray)
        {
            // we need these to do anything, so return early if missing
            if (_activePlacement == null || _dragTarget == null || _activeComponent==null) return;

            if (drawDebug)
            {
                Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, Color.orange, 0.5f);
            }
            ResolveSurfaceFromCursor(ray);

            ResolveSurfacesAboveBelow();

            // Free Drag Target Point Work
            Vector3 targetPoint = GetSurfaceProjectedPoint(ray);
            if (_hasCursorAnchor)
            {
                targetPoint.x = _cursorXZAnchor.x;
                targetPoint.z = _cursorXZAnchor.z;
            }
            targetPoint = ApplySurfaceHeightCorrection(targetPoint);
            _dragTarget.position = targetPoint;
            
            // Socket Hover Work
            newHover = null;
            var hits = Physics.RaycastAll(ray, maxRayDistance, placementMask);

            if (hits == null || hits.Length == 0)
                return;

            foreach (var hit in hits)
            {
                // Socket might be anywhere in the hierarchy
                if (!hit.collider.TryGetComponent(out FP_PlacementSocketComponent socket))
                    continue;

                if (drawDebug)
                {
                    //Debug.Log($"[Placement] Ray hit socket candidate: {socket.name}");
                }

                if (!socket.CanAccept(_activeComponent))
                    continue;

                newHover = socket;
                break; // first valid socket wins break out
            }
            if (_previousHoverSocket != newHover)
            {
                if (_previousHoverSocket != null)
                    _previousHoverSocket.SetHoverState(false);

                if (newHover != null)
                    newHover.SetHoverState(true);

                _previousHoverSocket = newHover;
            }
            _hoverSocket = newHover;
            
            // Magnetism or null

            if (_hoverSocket != null)
            {
                // ApplySocketMagnetism();
                _activeSocket = _hoverSocket;
                // Socket Hover Work
                if (!useSocketMagnet)
                {
                    // not using magnetism, just snap to socket
                    Transform target = _activeComponent.RootPlacement;

                    target.SetPositionAndRotation(
                        _hoverSocket.transform.position,
                        _hoverSocket.transform.rotation
                    );
                    // kick out
                    return;
                }
                // baseline position from free drag
                Vector3 surfacePos = _dragTarget.position;
                Quaternion surfaceRot = _dragTarget.rotation;
                // Socket Target
                Vector3 socketPos = _hoverSocket.transform.position;
                Quaternion socketRot = _hoverSocket.transform.rotation;
                // Distance to socket
                float dist = Vector3.Distance(surfacePos, socketPos);
                if (dist > magnetRange)
                {
                    // kick out we failed on distance check, no magnetism applied
                    return;
                }
                //normalized pull factor (0 far, 1 close)
                float t = 1f - (dist / magnetRange);

                //smooth easing
                t = t * t;

                // Blend Position

                Vector3 blendPos = Vector3.Lerp(surfacePos, socketPos, t);

                //blend rotation? 
                //Quaternion blendedRot = Quaternion.Slerp(surfaceRot,socketRot,t);

                _dragTarget.position = Vector3.Lerp(surfacePos, blendPos, Time.deltaTime * magnetStrength);
                //_dragTarget.rotation = Quaternion.Slerp(surfaceRot,blendedRot,Time.deltaTime*magnetStrength);

                // really close snap it in
                if (dist < magnetSnapDistance)
                {
                    _dragTarget.SetPositionAndRotation(socketPos, socketRot);
                }

                
            }
            else
            {
                _activeSocket = null;
            }
        }
        #region Drag Details
        protected void ResolveSurfacesAboveBelow()
        {
            FP_PlacementSide bottom = _activeComponent.GetBottomSide;
            if (bottom == null)
                return;
            //going to just go up double the distance we need to cast down so we can get stacked items
            float verticalOffsetY = surfaceCastDistance *surfaceCastVertialOffsetScalar;
            
            Vector3 origin = bottom.transform.position + (verticalOffsetY*Vector3.up);
            Vector3 halfExtents = new Vector3(
                bottom.SurfaceSize.x * 0.5f,
                0.05f,
                bottom.SurfaceSize.y * 0.5f
            );
            
            RaycastHit[] hits = Physics.BoxCastAll(
                origin,
                halfExtents,
                Vector3.down,
                bottom.transform.rotation,
                verticalOffsetY*2f,
                surfaceMask
            );
            
            if (drawDebug && fpDrawer != null)
            {
                fpDrawer.DrawBox(origin, Quaternion.identity, halfExtents*2f, Color.green, 1f);
                fpDrawer.DrawBox(origin + Vector3.down * verticalOffsetY*2f, Quaternion.identity, halfExtents * 2f, Color.green, 1f);
                //Debug.Log($"BoxCast from {origin} down {verticalOffsetY*2f} found {hits.Length} hits");
            }
            if (hits == null || hits.Length == 0)
                return;

            RaycastHit bestHit = default;

            float highestPt = -100000f;
            PlacementObjectComponent hitPlacement = null;
            for(int i=0;i<hits.Length;i++)
            {
                var hit = hits[i];
                if (hit.collider.transform.IsChildOf(_activeComponent.transform))
                    continue;
                var Placement = hit.collider.gameObject.GetComponent<PlacementObjectComponent>();
                if(Placement != null)
                {
                    //Debug.Log($"Hit placement object: {Placement.name}");
                    // location of top
                    if (Placement.GetTopSide != null)
                    {
                        if(Placement.GetTopSide.transform.position.y>=highestPt)
                        {
                            bestHit = hit;
                            highestPt = Placement.GetTopSide.transform.position.y;
                            hitPlacement = Placement;
                            if (drawDebug)
                            {
                                Debug.Log($"New Best Hit: {Placement.name} at height {highestPt}");
                            }
                        }
                    }
                }
            }
            if(bestHit.collider == null)
                return;
            if(hitPlacement != null)
            {
                _currentSurface = hitPlacement.GetTopSide.transform;
                _currentSurfacePlane = new Plane(hitPlacement.GetTopSide.Normal.normalized,bestHit.point);
                if(drawDebug && fpDrawer != null)
                {
                    fpDrawer.DrawPlane(bestHit.point,Quaternion.identity,new Vector2(2,2), Color.blue, 2f);
                }
            }
            else
            {
                _currentSurface = bestHit.collider.transform;
                _currentSurfacePlane = new Plane(bestHit.normal, bestHit.point);
            }
        }

        /// <summary>
        /// Updates cursor XZ anchor point if surface assist is enabled
        /// </summary>
        /// <param name="ray"></param>
        protected void ResolveSurfaceFromCursor(Ray ray)
        {
            _hasCursorAnchor = false;

            if (!useCursorSurfaceAssist)
                return;

            if (Physics.Raycast(ray, out RaycastHit hit, cursorAssistMaxDistance, perspectiveSurface))
            {
                // Ignore self && other 
                if (_activeComponent != null &&
                    hit.collider.transform.IsChildOf(_activeComponent.transform))
                    return;
                // Store intended XZ target
                _cursorXZAnchor = hit.point;
                _hasCursorAnchor = true;

                if (drawDebug)
                {
                    Debug.DrawLine(ray.origin, hit.point, Color.magenta, 0.5f);
                }
            }
        }

        protected Vector3 GetSurfaceProjectedPoint(Ray ray)
        {
            if (_currentSurface == null)
            {
                // fallback to original camera depth
                return ray.GetPoint(_dragRayDistance) + _dragLocalOffset;
            }
            
            if (_currentSurfacePlane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter) + _dragLocalOffset;
            }
            
            return _dragTarget.position;
        }
        Vector3 ApplySurfaceHeightCorrection(Vector3 targetPoint)
        {
            FP_PlacementSide bottom = _activeComponent.GetBottomSide;

            if (bottom == null)
                return targetPoint;

            float bottomOffset =
                bottom.transform.position.y - _dragTarget.position.y;
            // remove local drag offset to get the true bottom height relative pivot
            float extentHieghtYOffset = _dragTarget.position.y-bottom.transform.position.y-_dragLocalOffset.y; 
            targetPoint.y += extentHieghtYOffset;
            return targetPoint;
        }
        
        #endregion
        protected override void OnDragEnded()
        {
            if (_activePlacement == null)
                return;
            if(_activeComponent == null)
                return;
            if (_activeSocket != null)
            {
                if(_activeSocket.gameObject.TryGetComponent(out IFPPlacementSocket socketInt))
                {
                    socketInt.OnPlacementInSocket(_activeSocket,_activeComponent, _activeComponent.RootPlacement);
                }
                
                if(_activeComponent.gameObject.TryGetComponent(out IFPPlacementSocket placementInt))
                {
                    placementInt.OnPlacementInSocket(_activeSocket,_activeComponent,_activeComponent.RootPlacement);
                }
                dragEndSocketSuccessEvent?.Invoke(_activeComponent, _activeSocket);
            }
            else
            {
                //No socket : surface drop allowed
                Vector3 dropPos = _activeComponent.transform.position;

                if (!IsWithinBounds(dropPos))
                {
                    // Out of bounds → return home
                    if(_activeComponent.gameObject.TryGetComponent(out IFPPlacementSocket placementInt))
                    {
                        placementInt.OnPlacementOutOfBounds(_startPos, _startRot);
                    }
                }
                else
                {
                    // In bounds → keep dropped position
                    if (drawDebug)
                    {
                        Debug.Log("[Placement] Dropped on surface (no socket). Keeping position.");
                    }
                    dragEndMovedLocationEvent?.Invoke(_activeComponent, null);
                    if (_activeComponent.gameObject.TryGetComponent(out IFPPlacementSocket placementInt))
                    {
                        placementInt.OnGeneralPlacement(_startPos, _startRot);
                    }
                }
                dragEndSocketFailedEvent?.Invoke(_activeComponent, null);
            }
            if(_previousHoverSocket != null)
            {
                _previousHoverSocket.SetHoverState(false);
            }
            //check to see if we need to update our surface cast distance for next drag
            UpdateBoxCastSurfaceHeight();
            Debug.Log($"End Drag Complete?");
            _activePlacement = null;
            _activeComponent = null;
            _activeSocket = null;
            _hoverSocket = null;
            _dragTarget = null;
            _previousHoverSocket = null;
            //_dragStarted = false;
            //_dragOccurred = false;
            _dragLocalOffset = Vector3.zero;
        }
        protected bool IsWithinBounds(Vector3 position)
        {
            if (!enforceBounds)
                return true;

            Bounds b = new Bounds(boundsCenter, boundsSize);
            return b.Contains(position);
        }
        private void OnDrawGizmosSelected()
        {
            if (!enforceBounds)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }

        #endregion
        /// <summary>
        /// Changes the Surface Cast Distance based on reference points and the last known location of the active component
        /// </summary>
        private void UpdateBoxCastSurfaceHeight()
        {
            if(_activeComponent==null || lowestGroundPointRef==null)
            {
                return;
            }
            if (lowestGroundPointRef != null)
            {
                if (_activeComponent.RootPlacement.position.y > lowestGroundPointRef.position.y)
                {
                    float neededDistance = _activeComponent.RootPlacement.position.y - lowestGroundPointRef.position.y + 1f;
                    if (neededDistance > surfaceCastDistance)
                    {
                        surfaceCastDistance = neededDistance;
                    }
                }
                else
                {
                    float neededDistance = 1 + (lowestGroundPointRef.position.y - _activeComponent.RootPlacement.position.y);
                    if (neededDistance > surfaceCastDistance)
                    {
                        surfaceCastDistance = neededDistance;
                    }
                }
                /// ensure we are 3 times the height of the object we are dragging and positive.
                surfaceCastDistance = Mathf.Max(surfaceCastDistance, _activeComponent.ReturnHeightValue * surfaceCastVertialOffsetScalar*2f);
            }
        }
        /// <summary>
        /// What we want to do via Double Click
        /// </summary>
        /// <param name="worldPos"></param>
        protected override void OnPrimaryDoubleClick(Vector3 worldPos)
        {
            //general event
            //Debug.Log($"Double Click Event Invoked on {_clickedComponent?.name} at Socket: {_activeSocket?.name}");
            var otherClickedComponent = FindClickComponent(worldPos);
            if (_clickedComponent != null&&otherClickedComponent!=null)
            {
                if (otherClickedComponent == _clickedComponent)
                {
                    doubleClickEvent?.Invoke(_clickedComponent, _activeSocket);
                    if (_clickedComponent.TryGetComponent(out IFPInteractionClicks clickAction))
                    {
                        clickAction.OnDoubleClickAction();
                    }
                }
            }
            _clickedComponent = null;
        }
        protected override void OnPrimaryClick(Vector3 worldPos)
        {
            _clickedComponent = FindClickComponent(worldPos);
            if (_clickedComponent == null) return;

            singleClickEvent?.Invoke(_clickedComponent, _activeSocket);
            if(_clickedComponent.TryGetComponent(out IFPInteractionClicks clickAction))
            {
                clickAction.OnSingleClickAction();
            }
        }
        protected PlacementObjectComponent FindClickComponent(Vector3 worldPosRayEnd)
        {
            Ray newRay = new Ray(targetCamera.transform.position, (worldPosRayEnd - targetCamera.transform.position).normalized);
            var allHits = Physics.RaycastAll(newRay, maxRayDistance, placementMask);
            if (allHits == null || allHits.Length == 0) return null;
            for (int i = 0; i < allHits.Length; i++)
            {
                var hit = allHits[i];
                if (hit.collider.TryGetComponent(out PlacementObjectComponent poc))
                {
                    if (poc.Clickable)
                    {
                        return poc;
                    }
                    /*
                    if (poc.Locked)
                    {

                    }
                    else
                    {
                        return poc;
                    }
                    */
                    
                }
            }
            return null;
        }
        protected override void ForceRelease()
        {
            base.ForceRelease();
            //_dragStarted = false;
        }
    }
}
