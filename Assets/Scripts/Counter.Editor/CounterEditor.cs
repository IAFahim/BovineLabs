using Counter.Authoring;
using UnityEditor;

namespace Counter.Editor
{
    [CustomEditor(typeof(CounterAuthoring))]
    public class CounterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var authoring = (CounterAuthoring)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Counter Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Initial Value: {authoring.initialValue}");
        }
    }
}