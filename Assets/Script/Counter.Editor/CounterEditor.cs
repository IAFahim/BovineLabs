using Examples._01_AssemblyArchitecture.Script.Counter.Authoring;
using UnityEditor;

namespace Examples._01_AssemblyArchitecture.Counter.Editor
{
    [CustomEditor(typeof(CounterAuthoring))]
    public class CounterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CounterAuthoring authoring = (CounterAuthoring)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Counter Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Initial Value: {authoring.initialValue}");
        }
    }
}
