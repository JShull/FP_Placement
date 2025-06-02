namespace FuzzPhyte.Placement
{
    using UnityEngine;
    using System.Collections.Generic;
    using FuzzPhyte.Utility;

    public enum StackSuitability
    {
        Heavy,
        Medium,
        Light,
        Fragile
    }
    
    public enum ShapeType
    {
        Circle,
        Ellipse,
        Rectangle
    }

    [System.Serializable]
    [CreateAssetMenu(fileName = "PlacementObject", menuName = "FuzzPhyte/Placement/Object", order = 11)]
    public class PlacementObject : FP_Data
    {
        [Header("Placement Settings")]
        public Vector3 Normal = Vector3.up;
        public List<Vector2> FootprintPoints = new();
        public float FootprintSize = 0.5f;
        public Bounds BoundingBox = new(Vector3.zero, Vector3.one);

        [Header("Stacking Area Settings")]
        public bool Stackable = true;
        public StackSuitability StackSuitability = StackSuitability.Medium;
        public ShapeType Shape = ShapeType.Circle;
        public Vector2 StackSize = new Vector2(0.5f, 0.5f);
        public Vector3 StackCenterOffset = Vector3.up * 0.5f;
        
        [Header("Category Links")]
        public List<PlacementCategory> Categories = new(); // CEFR level, tags, vocab category
        public List<string> VocabularyTags = new(); // Optional tags for vocabulary or thematic grouping
        
        public Vector3 GetRandomPointLocal()
        {
            Vector2 local2D = Vector2.zero;
            switch (Shape)
            {
                case ShapeType.Circle:
                    float angleC = Random.Range(0f, Mathf.PI * 2f);
                    float radiusC = Random.Range(0f, StackSize.x);
                    local2D = new Vector2(Mathf.Cos(angleC), Mathf.Sin(angleC)) * radiusC;
                    break;
                case ShapeType.Ellipse:
                    float angleE = Random.Range(0f, Mathf.PI * 2f);
                    float rE = Mathf.Sqrt(Random.Range(0f, 1f)); // Uniform distribution
                    local2D = new Vector2(rE * StackSize.x * Mathf.Cos(angleE), rE * StackSize.y * Mathf.Sin(angleE));
                    break;
                case ShapeType.Rectangle:
                    local2D = new Vector2(
                        Random.Range(-StackSize.x * 0.5f, StackSize.x * 0.5f),
                        Random.Range(-StackSize.y * 0.5f, StackSize.y * 0.5f)
                    );
                    break;
            }
            return StackCenterOffset + new Vector3(local2D.x, 0f, local2D.y);
        }

    }
}
