namespace FuzzPhyte.Placement.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(FuzzPhyte.Placement.PlacementVolumeComponent))]
    public class PlacementVolumeComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Generate PlacementVolume Data"))
            {
                var component = (FuzzPhyte.Placement.PlacementVolumeComponent)target;
                var volume = component.GeneratePlacementVolume();
                if (volume != null)
                {
                    Debug.Log($"Generated PlacementVolume for '{component.VolumeName}': {volume.Bounds}");
                }
            }
        }
    }
}