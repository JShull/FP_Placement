namespace FuzzPhyte.Placement
{
    using UnityEditor;
    using UnityEngine;
    using FuzzPhyte.Utility;
    
    public class PlacementObjectBuilderWindow : EditorWindow
    {
        protected GameObject targetObject;
        protected string objectName = "NewPlacementObject";
        private PlacementCategory defaultCategory;
        protected Vector3 boundsCenter;
        protected Vector3 boundsSize = Vector3.one;
        protected bool autoFitBounds = true;
        protected StackSuitability stackSuitability = StackSuitability.Medium;
        protected ShapeType stackShape = ShapeType.Circle;
        protected Vector2 stackSize = new(0.5f, 0.5f);
        protected Vector3 stackCenterOffset = Vector3.up * 0.5f;

        [MenuItem("FuzzPhyte/Placement/Placement Object Builder", priority = FuzzPhyte.Utility.FP_UtilityData.ORDER_SUBMENU_LVL5)]
        public static void ShowWindow()
        {
            GetWindow<PlacementObjectBuilderWindow>("FP Placement Object Builder");
        }

        private void OnGUI()
        {
            GUILayout.Label("Placement Object Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
            objectName = EditorGUILayout.TextField("PlacementObject Name", objectName);

            EditorGUILayout.Space();
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
        }
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
    }
}
