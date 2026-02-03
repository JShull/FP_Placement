namespace FuzzPhyte.Placement.Editor
{
    using UnityEditor;
    using UnityEngine;
    using FuzzPhyte.Placement.Interaction;
    using System.Collections.Generic;
    public class PlacementObjectBuilderWindow : EditorWindow
    {
        protected GameObject targetObject;
        protected Transform layoutParentOverride;

        protected string objectName = "NewPlacementObject";
        private PlacementCategory defaultCategory;

        protected Vector3 boundsCenter;
        protected Vector3 boundsSize = Vector3.one;
        protected bool autoFitBounds = true;

        protected PlacementBuildMode buildMode = PlacementBuildMode.Stacking;

        #region Stacking
        protected StackSuitability stackSuitability = StackSuitability.Medium;
        protected ShapeType stackShape = ShapeType.Circle;
        protected Vector2 stackSize = new(0.5f, 0.5f);
        protected Vector3 stackCenterOffset = Vector3.up * 0.5f;
        #endregion


        #region Layout
        protected Transform layoutAnchor;
        protected bool useSelectionAsFallbackAnchor = true;

        protected LayoutSurfaceType layoutSurface = LayoutSurfaceType.SphereSurface;
        protected LayoutDistribution layoutDistribution = LayoutDistribution.Even;
        protected int layoutSeed = 12345;
        protected bool layoutOrientOutward = true;

        protected SphereEvenMode sphereEvenMode = SphereEvenMode.Fibonacci;
        protected int latLonRingCount = 8;

        protected float sphereRadius = 1.0f;
        protected Vector2 thetaRangeDeg = new Vector2(0f, 180f);
        protected Vector2 phiRangeDeg = new Vector2(0f, 360f);

        protected Vector3 boxSize = Vector3.one;
        protected Vector3 boxCenterOffset = Vector3.zero;

        #region Mesh Surface Layout Parameters
        protected GameObject meshSurfaceSource;
        protected bool meshRemoveDuplicateVertices = true;
        protected float meshDuplicateEpsilon = 0.0001f;
        protected MeshVertexPickMode meshPickMode = MeshVertexPickMode.EvenInOrder; // uses your renamed enum
        protected bool meshIncludeSkinned = true;

        //bounds related

        protected BoxCollider meshBoundsFilter;
        protected bool meshIncludeBoundary = false;
        protected bool meshInvertBoundsFilter = false;

        #endregion
        #endregion

        [MenuItem("FuzzPhyte/Placement/Placement Object Builder", priority = FuzzPhyte.Utility.FP_UtilityData.ORDER_SUBMENU_LVL5)]
        public static void ShowWindow()
        {
            GetWindow<PlacementObjectBuilderWindow>("FP Placement Object Builder");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            // Only draw when MeshSurface + a bounds filter is assigned
            if (layoutSurface != LayoutSurfaceType.MeshSurface) return;
            if (meshBoundsFilter == null) return;

            DrawBoundsFilterGizmo(meshBoundsFilter, meshIncludeBoundary,meshInvertBoundsFilter);
        }

        private static void DrawBoundsFilterGizmo(BoxCollider box, bool includeBoundary, bool meshBoundsInvert)
        {
            if (box == null) return;

            if (!includeBoundary)
            {
                return;
            }
            // Cyan
            if (meshBoundsInvert)
            {
                Handles.color = Color.red;
            }
            else
            {
                Handles.color = Color.cyan;
            }
               

            // Build matrix that represents the oriented box in world space:
            // Position = collider center in world, Rotation = transform rotation, Scale = transform lossyScale
            Transform t = box.transform;

            Vector3 worldCenter = t.TransformPoint(box.center);
            Quaternion worldRot = t.rotation;
            Vector3 worldSize = Vector3.Scale(box.size, t.lossyScale);

            // Draw wire cube with matrix so rotation is correct
            Matrix4x4 prev = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(worldCenter, worldRot, Vector3.one);

            Handles.DrawWireCube(Vector3.zero, worldSize);

            // Optional: label
            Vector3 labelPos = worldCenter + (t.up * (worldSize.y * 0.5f + 0.05f));
            //string label = includeBoundary ? "Bounds Filter (Include Boundary)" : "Bounds Filter";
            string label = meshBoundsInvert
    ? (includeBoundary ? "Bounds Filter (Outside, Include Boundary)" : "Bounds Filter (Outside)")
    : (includeBoundary ? "Bounds Filter (Inside, Include Boundary)" : "Bounds Filter (Inside)");

            Handles.Label(labelPos, label);

            Handles.matrix = prev;

            // Force SceneView repaint so it updates even if nothing is selected
            SceneView.RepaintAll();
        }

        private void OnGUI()
        {
            GUILayout.Label("Placement Object Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
            objectName = EditorGUILayout.TextField("PlacementObject Name", objectName);

            buildMode = (PlacementBuildMode)EditorGUILayout.EnumPopup("Build Mode", buildMode);

            EditorGUILayout.Space();
            

            if (buildMode == PlacementBuildMode.Stacking)
            {
                
                DrawStackingUI();
            }
            else
            {
                DrawLayoutUI();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Creates a PlacementObject asset and links it to the Target GameObject via PlacementObjectComponent.", MessageType.Info);

            using (new EditorGUI.DisabledScope(targetObject == null))
            {
                if (GUILayout.Button("Create/Save PlacementObject Asset"))
                {
                    CreatePlacementObjectAsset();
                }
            }
            /*
            GUILayout.Label("Stacking Settings", EditorStyles.boldLabel);
            stackSuitability = (StackSuitability)EditorGUILayout.EnumPopup("Stack Suitability", stackSuitability);
            stackShape = (ShapeType)EditorGUILayout.EnumPopup("Stack Shape", stackShape);
            stackSize = EditorGUILayout.Vector2Field("Stack Size", stackSize);
            stackCenterOffset = EditorGUILayout.Vector3Field("Stack Center Offset", stackCenterOffset);

            EditorGUILayout.Space();
            GUILayout.Label("Optional", EditorStyles.boldLabel);
            defaultCategory = (PlacementCategory)EditorGUILayout.ObjectField("Default Category", defaultCategory, typeof(PlacementCategory), false);

            EditorGUILayout.Space();
            GUI.enabled = targetObject != null;

            if (GUILayout.Button("Auto-Fit Bounds from MeshRenderer"))
            {
                AutoFitBounds();
            }

            if (GUILayout.Button("Generate PlacementObject"))
            {
                GeneratePlacementObject();
            }

            GUI.enabled = true;
            */
        }
        private void DrawStackingUI()
        {
            GUILayout.Label("Stacking Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            autoFitBounds = EditorGUILayout.Toggle("Auto Fit Bounds", autoFitBounds);

            if (autoFitBounds && targetObject != null)
            {
                var r = targetObject.GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    boundsCenter = r.bounds.center;
                    boundsSize = r.bounds.size;
                }
                else
                {
                    boundsCenter = targetObject.transform.position;
                    boundsSize = Vector3.one;
                }
            }
            else
            {
                boundsCenter = EditorGUILayout.Vector3Field("Bounds Center", boundsCenter);
                boundsSize = EditorGUILayout.Vector3Field("Bounds Size", boundsSize);
            }

            EditorGUILayout.Space();

            defaultCategory = (PlacementCategory)EditorGUILayout.ObjectField
                (
                    "Default Category",
                    defaultCategory,
                    typeof(PlacementCategory),
                    false
                );


            stackSuitability = (StackSuitability)EditorGUILayout.EnumPopup("Stack Suitability", stackSuitability);
            stackShape = (ShapeType)EditorGUILayout.EnumPopup("Stack Shape", stackShape);

            stackSize = EditorGUILayout.Vector2Field("Stack Size", stackSize);
            stackCenterOffset = EditorGUILayout.Vector3Field("Stack Center Offset", stackCenterOffset);
        }

        private void DrawLayoutUI()
        {
            GUILayout.Label("Layout Settings (3D)", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            layoutAnchor = (Transform)EditorGUILayout.ObjectField
                (
                "Layout Anchor",
                layoutAnchor,
                typeof(Transform),
                true
                );


            GUILayout.Space(10f);
            EditorGUILayout.LabelField(
                "Selection as Fallback Anchor",
                GUILayout.MinWidth(10f) // adjust as needed
            );

            useSelectionAsFallbackAnchor = EditorGUILayout.Toggle(
                useSelectionAsFallbackAnchor,
                GUILayout.Width(20f)
            );

            EditorGUILayout.EndHorizontal();
            layoutParentOverride = (Transform)EditorGUILayout.ObjectField
                (
                "Layout Parent",
                layoutParentOverride,
                typeof(Transform),
                true
                );

            layoutSurface = (LayoutSurfaceType)EditorGUILayout.EnumPopup("Layout Surface", layoutSurface);

            layoutDistribution = (LayoutDistribution)EditorGUILayout.EnumPopup("Distribution", layoutDistribution);
            if (layoutDistribution == LayoutDistribution.Random)
            {
                layoutSeed = EditorGUILayout.IntField("Seed", layoutSeed);
            }
            layoutOrientOutward = EditorGUILayout.Toggle("Orient Outward", layoutOrientOutward);
            EditorGUILayout.Space();
            if (layoutSurface == LayoutSurfaceType.SphereSurface)
            {

                sphereRadius = EditorGUILayout.FloatField("Sphere Radius", sphereRadius);
                if (layoutDistribution == LayoutDistribution.Even)
                {
                    sphereEvenMode = (SphereEvenMode)EditorGUILayout.EnumPopup("Even Mode", sphereEvenMode);

                    if (sphereEvenMode == SphereEvenMode.LatLonRings)
                    {
                        latLonRingCount = EditorGUILayout.IntField("Ring Count", latLonRingCount);
                        latLonRingCount = Mathf.Max(2, latLonRingCount);
                        EditorGUILayout.Space();
                    }
                }
                
                thetaRangeDeg = EditorGUILayout.Vector2Field("Theta Range (deg)", thetaRangeDeg);
                phiRangeDeg = EditorGUILayout.Vector2Field("Phi Range (deg)", phiRangeDeg);
            }
            else if(layoutSurface == LayoutSurfaceType.BoxSurface)
            {
                boxSize = EditorGUILayout.Vector3Field("Box Size", boxSize);
                boxCenterOffset = EditorGUILayout.Vector3Field("Box Center Offset", boxCenterOffset);
            }
            else
            {
                GUILayout.Label("Mesh Surface Settings", EditorStyles.boldLabel);

                meshSurfaceSource = (GameObject)EditorGUILayout.ObjectField(
                    "Mesh Source",
                    meshSurfaceSource,
                    typeof(GameObject),
                    true
                );

                meshIncludeSkinned = EditorGUILayout.Toggle("Include Skinned", meshIncludeSkinned);

                meshRemoveDuplicateVertices = EditorGUILayout.Toggle("Remove Duplicates", meshRemoveDuplicateVertices);
                using (new EditorGUI.DisabledScope(!meshRemoveDuplicateVertices))
                {
                    meshDuplicateEpsilon = EditorGUILayout.FloatField("Duplicate Epsilon", meshDuplicateEpsilon);
                    meshDuplicateEpsilon = Mathf.Max(0.0000001f, meshDuplicateEpsilon);
                }
                meshIncludeBoundary = EditorGUILayout.Toggle("Include Boundary", meshIncludeBoundary);
                if (meshIncludeBoundary)
                {
                    meshInvertBoundsFilter = EditorGUILayout.Toggle("Invert Bounds Filter", meshInvertBoundsFilter);

                    meshBoundsFilter = (BoxCollider)EditorGUILayout.ObjectField(
                        "Bounds Filter (BoxCollider)",
                        meshBoundsFilter,
                        typeof(BoxCollider),
                        true
                    );

                }
                // Pick mode: ties to your "Even/Random" concepts
                // If you want it to follow layoutDistribution automatically, you can hide this and derive it.
                //meshPickMode = (MeshVertexPickMode)EditorGUILayout.EnumPopup("Pick Mode", meshPickMode);

                // Optional quality-of-life: keep in sync with your Distribution dropdown
                if (layoutDistribution == LayoutDistribution.Even)
                    meshPickMode = MeshVertexPickMode.EvenInOrder;
                else if (layoutDistribution == LayoutDistribution.Random)
                    meshPickMode = MeshVertexPickMode.Random;
            }

                EditorGUILayout.Space();

            bool hasAnchor =
                layoutAnchor != null ||
                (useSelectionAsFallbackAnchor && Selection.activeTransform != null) ||
                targetObject != null;

            bool hasParent =
                layoutParentOverride != null ||
                targetObject != null ||
                (useSelectionAsFallbackAnchor && Selection.activeTransform != null);

            bool hasMeshSource = layoutSurface != LayoutSurfaceType.MeshSurface || meshSurfaceSource != null;
            
            using (new EditorGUI.DisabledScope(!(hasAnchor || hasParent || hasMeshSource)))
            {
                if (GUILayout.Button("Apply Layout To Children"))
                {
                    ApplyLayoutToChildren();
                }
            }
        }

        private void CreatePlacementObjectAsset()
        {
            if (targetObject == null)
            {
                Debug.LogError("No target object selected.");
                return;
            }

            var placementObj = CreateInstance<PlacementObject>();

            // Common
            placementObj.BuildMode = buildMode;
            placementObj.BoundingBox = new Bounds(boundsCenter, boundsSize);

            // Stacking
            placementObj.StackSuitability = stackSuitability;
            placementObj.Shape = stackShape;
            placementObj.StackSize = stackSize;
            placementObj.StackCenterOffset = stackCenterOffset;
            placementObj.Stackable = true;

            // Layout
            placementObj.LayoutSurface = layoutSurface;
            placementObj.LayoutDistribution = layoutDistribution;
            placementObj.LayoutSeed = layoutSeed;
            placementObj.LayoutOrientOutward = layoutOrientOutward;

            placementObj.SphereRadius = sphereRadius;
            placementObj.ThetaRangeDeg = thetaRangeDeg;
            placementObj.PhiRangeDeg = phiRangeDeg;

            placementObj.BoxSize = boxSize;
            placementObj.BoxCenterOffset = boxCenterOffset;

            placementObj.SphereEvenMode = sphereEvenMode;
            placementObj.LatLonRingCount = latLonRingCount;

            // mesh settings
            if (meshSurfaceSource != null)
            {
                var meshFilter = meshSurfaceSource.GetComponent<MeshFilter>();
                if(meshFilter != null)
                {
                    var mesh = meshFilter.sharedMesh;
                    placementObj.MeshSurfaceSource = mesh;
                }
                else
                {
                    var meshSkinnedFilter = meshSurfaceSource.GetComponent<SkinnedMeshRenderer>();
                    if(meshSkinnedFilter != null)
                    {
                        placementObj.MeshSurfaceSource = meshSkinnedFilter.sharedMesh;
                    }
                }
                placementObj.MeshRemoveDuplicateVertices = meshRemoveDuplicateVertices;
                placementObj.MeshDuplicateEpsilon = meshDuplicateEpsilon;
                placementObj.MeshPickMode = meshPickMode;
                placementObj.MeshIncludeSkinned = meshIncludeSkinned;
            }

            // Category default
            placementObj.Categories.Clear();
            if (defaultCategory != null)
            {
                placementObj.Categories.Add(defaultCategory);
            }

            string path = EditorUtility.SaveFilePanelInProject("Save PlacementObject", objectName, "asset", "Save PlacementObject");
            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(placementObj);
                return;
            }

            AssetDatabase.CreateAsset(placementObj, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = placementObj;

            // Attach MonoBehaviour to the targetObject
            var component = targetObject.GetComponent<PlacementObjectComponent>();
            if (component == null)
                component = Undo.AddComponent<PlacementObjectComponent>(targetObject);

            Undo.RecordObject(component, "Assign Placement Data");
            component.PlacementData = placementObj;
            EditorUtility.SetDirty(component);

            Debug.Log($"PlacementObject '{objectName}' generated and linked to {targetObject.name}.");
        }

        private void ApplyLayoutToChildren()
        {
            // Resolve parent (the thing whose children we layout)
            Transform parent =
                layoutParentOverride != null ? layoutParentOverride :
                (targetObject != null ? targetObject.transform :
                (useSelectionAsFallbackAnchor ? Selection.activeTransform : null));

            if (parent == null)
            {
                Debug.LogWarning("No Layout Parent found. Assign Layout Parent or select a Transform in the scene.");
                return;
            }
            Transform anchor =
                layoutAnchor != null ? layoutAnchor :
                (targetObject != null ? targetObject.transform :
                (useSelectionAsFallbackAnchor ? Selection.activeTransform : null));

            if (anchor == null)
            {
                Debug.LogWarning("No Layout Anchor found. Assign Layout Anchor or select a Transform in the scene.");
                return;
            }
            //mesh mode
            List<Vector3> meshWorldVerts = null;
            List<Vector3> meshWorldNormals = null;
            if (layoutSurface == LayoutSurfaceType.MeshSurface)
            {
                
                if (meshSurfaceSource == null)
                {
                    Debug.LogWarning("Mesh Surface selected but no Mesh Source assigned.");
                    return;
                }

                if (!MeshVertexLayoutUtility.TryGetWorldVerticesAndNormals(
                    meshSurfaceSource,
                    out meshWorldVerts,
                    out meshWorldNormals,
                    includeSkinned: meshIncludeSkinned,
                    removeDuplicates: meshRemoveDuplicateVertices,
                    duplicateEpsilon: meshDuplicateEpsilon
                    ) || meshWorldVerts == null || meshWorldVerts.Count == 0)
                {
                    Debug.LogWarning($"Mesh Source '{meshSurfaceSource.name}' has no usable vertices.");
                    return;
                }
                if (meshBoundsFilter != null && meshIncludeBoundary)
                {
                    MeshVertexLayoutUtility.FilterByBoxColliderInPlace(
                        meshWorldVerts,
                        meshWorldNormals,
                        meshBoundsFilter,
                        includeBoundary: meshIncludeBoundary,
                        invert:meshInvertBoundsFilter
                    );

                    if (meshWorldVerts.Count == 0)
                    {
                        Debug.LogWarning("Mesh vertices were filtered out completely by the bounds filter.");
                        return;
                    }
                }

            }

            var children = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                children.Add(parent.GetChild(i));
            }

            if (children.Count == 0)
            {
                Debug.LogWarning("No child items found to layout.");
                return;
            }
            Vector3 anchorPos = anchor.position;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

           
            if (layoutSurface == LayoutSurfaceType.MeshSurface)
            {
                // Undo record for all children as a batch (since placer loops internally)
                foreach (var child in children)
                {
                    if (child != null)
                        Undo.RecordObject(child, "Apply Layout To Children");
                }

                MeshVertexPlacer.ApplyToTransforms(
                    children,
                    meshWorldVerts,
                    meshPickMode,                 // <-- uses the user's option (derived from Distribution in your UI)
                    orientToNormal: layoutOrientOutward,
                    worldNormals: meshWorldNormals,
                    randomSeed: layoutSeed
                );

                foreach (var child in children)
                {
                    if (child != null)
                        EditorUtility.SetDirty(child);
                }

                Undo.CollapseUndoOperations(group);
                Debug.Log($"Applied {layoutSurface} layout to {children.Count} children under '{parent.name}'.");
                return;
            }
            for (int i = 0; i < children.Count; i++)
            {
                Transform t = children[i];
                Undo.RecordObject(t, "Apply Layout To Children");

                if (layoutSurface == LayoutSurfaceType.SphereSurface)
                {
                    var temp = ScriptableObject.CreateInstance<PlacementObject>();
                    temp.LayoutSurface = layoutSurface;
                    temp.LayoutDistribution = layoutDistribution;
                    temp.SphereEvenMode = sphereEvenMode;

                    temp.LatLonRingCount = latLonRingCount;
                    temp.LayoutSeed = layoutSeed;
                    temp.LayoutOrientOutward = layoutOrientOutward;

                    temp.SphereRadius = sphereRadius;
                    temp.ThetaRangeDeg = thetaRangeDeg;
                    temp.PhiRangeDeg = phiRangeDeg;



                    Vector3 local = temp.GetLayoutPointSphereLocal(i, children.Count);
                    DestroyImmediate(temp);

                    Vector3 world = anchorPos + local;
                    t.position = world;

                    if (layoutOrientOutward)
                    {
                        // Look at anchor (inward) then rotate 180 to face outward
                        t.LookAt(anchorPos);
                        t.Rotate(0f, 180f, 0f, Space.Self);
                    }
                }
                else if (layoutSurface == LayoutSurfaceType.BoxSurface)
                {
                    var temp = ScriptableObject.CreateInstance<PlacementObject>();
                    temp.LayoutDistribution = layoutDistribution;
                    temp.LayoutSeed = layoutSeed;
                    temp.BoxSize = boxSize;
                    temp.BoxCenterOffset = boxCenterOffset;

                    Vector3 normal;
                    Vector3 local = temp.GetLayoutPointBoxLocal(i, children.Count, out normal);
                    DestroyImmediate(temp);

                    Vector3 world = anchorPos + local;
                    t.position = world;

                    if (layoutOrientOutward)
                    {
                        // Face outward along the surface normal
                        if (normal.sqrMagnitude > 0.0001f)
                            t.rotation = Quaternion.LookRotation(normal.normalized, Vector3.up);
                    }
                }

                EditorUtility.SetDirty(t);
            }
            
            Undo.CollapseUndoOperations(group);
            Debug.Log($"Applied {layoutSurface} layout to {children.Count} children under '{parent.name}'.");
        }

        #region Older Code
        private void AutoFitBounds()
        {
            if (targetObject == null)
            {
                Debug.LogError("No target object assigned.");
                return;
            }

            var renderer = targetObject.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                var collider = targetObject.GetComponent<BoxCollider>() ?? targetObject.AddComponent<BoxCollider>();
                collider.center = renderer.bounds.center - targetObject.transform.position;
                collider.size = renderer.bounds.size;
                collider.isTrigger = true;

                Debug.Log("Bounds auto-fitted from MeshRenderer.");
            }
            else
            {
                Debug.LogWarning("No MeshRenderer found to fit bounds.");
            }
        }
        private void GeneratePlacementObject()
        {
            if (targetObject == null)
            {
                Debug.LogError("No target object assigned.");
                return;
            }

            // Create the PlacementObject SO
            var placementObj = ScriptableObject.CreateInstance<PlacementObject>();
            placementObj.name = objectName;
            placementObj.Normal = Vector3.up;
            placementObj.Stackable = true;
            placementObj.StackSuitability = stackSuitability;
            placementObj.Shape = stackShape;
            placementObj.StackSize = stackSize;
            placementObj.StackCenterOffset = stackCenterOffset;

            if (autoFitBounds)
            {
                var meshRenderer = targetObject.GetComponentInChildren<MeshRenderer>();
                if (meshRenderer != null)
                {
                    placementObj.BoundingBox = meshRenderer.bounds;
                }
                else
                {
                    placementObj.BoundingBox = new Bounds(targetObject.transform.position, Vector3.one);
                }
            }
            else
            {
                placementObj.BoundingBox = new Bounds(boundsCenter, boundsSize);
            }

            // Save the PlacementObject asset
            string path = EditorUtility.SaveFilePanelInProject("Save PlacementObject", objectName, "asset", "Save PlacementObject");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(placementObj, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = placementObj;

                // Attach MonoBehaviour to the targetObject
                var component = targetObject.GetComponent<PlacementObjectComponent>();
                if (component == null)
                    component = targetObject.AddComponent<PlacementObjectComponent>();

                component.PlacementData = placementObj;

                Debug.Log($"PlacementObject '{objectName}' generated and linked to {targetObject.name}.");
            }
        }
        #endregion
    }

    
}

