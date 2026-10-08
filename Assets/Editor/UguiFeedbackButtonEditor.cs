using Relicfall.UI;
using UnityEditor;
using UnityEditor.UI;

namespace Relicfall.Editor
{
    [CustomEditor(typeof(UguiFeedbackButton)), CanEditMultipleObjects]
    public sealed class UguiFeedbackButtonEditor : ButtonEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("交互反馈", EditorStyles.boldLabel);
            foreach (string property in new[] { "normalTint", "hoverColor", "chosenColor", "fadeDuration", "hoverScale", "pressedScale" })
                EditorGUILayout.PropertyField(serializedObject.FindProperty(property));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
