namespace FuzzPhyte.Placement.Tests
{
    using FuzzPhyte.Placement.OrbitalCamera;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class FPModelDisplayBindingTests
    {
        [Test]
        public void GetWorldBounds_UsesTransformedOverrideWhenEnabled()
        {
            var root = new GameObject("Model Root");
            var data = ScriptableObject.CreateInstance<FP_ModelDisplayData>();

            try
            {
                root.transform.position = new Vector3(10f, 0f, 0f);
                root.transform.localScale = Vector3.one * 2f;

                data.UseLocalBoundsOverride = true;
                data.BoundsCenter = new Vector3(1f, 0f, 0f);
                data.BoundsSize = new Vector3(2f, 4f, 6f);
                data.BoundsPadding = 1.5f;

                FP_ModelDisplayBinding binding = AddBinding(root, data);

                Bounds result = binding.GetWorldBounds();

                AssertVector3(new Vector3(12f, 0f, 0f), result.center);
                AssertVector3(new Vector3(6f, 12f, 18f), result.size);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void GetWorldBounds_UsesRendererBoundsWhenOverrideDisabled()
        {
            var root = new GameObject("Model Root");
            var data = ScriptableObject.CreateInstance<FP_ModelDisplayData>();

            try
            {
                data.UseLocalBoundsOverride = false;
                data.BoundsCenter = new Vector3(100f, 100f, 100f);
                data.BoundsSize = Vector3.one * 50f;

                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = new Vector3(3f, 0f, 0f);

                FP_ModelDisplayBinding binding = AddBinding(root, data);
                Bounds expected = child.GetComponent<Renderer>().bounds;

                Bounds result = binding.GetWorldBounds();

                AssertVector3(expected.center, result.center);
                AssertVector3(expected.size, result.size);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        private static FP_ModelDisplayBinding AddBinding(GameObject root, FP_ModelDisplayData data)
        {
            FP_ModelDisplayBinding binding = root.AddComponent<FP_ModelDisplayBinding>();
            var serializedBinding = new SerializedObject(binding);
            serializedBinding.FindProperty("_data").objectReferenceValue = data;
            serializedBinding.ApplyModifiedPropertiesWithoutUndo();
            return binding;
        }

        private static void AssertVector3(Vector3 expected, Vector3 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }
    }
}
