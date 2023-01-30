using System.Reflection;
using UnityEngine;
using UnityEditor;
using zapnet;

[CustomEditor(typeof(EntitySubsystem), true)]
public class EntitySubsystemInspector : Editor
{
    private bool _showSyncVars = false;

    public override void OnInspectorGUI()
    {
        _showSyncVars = EditorGUILayout.BeginFoldoutHeaderGroup(_showSyncVars, "Synchronized Variables");
        
        if (_showSyncVars)
        {
            var fieldFlags = (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldInfos = (target as EntitySubsystem).GetType().GetFields(fieldFlags);
            var baseInterface = typeof(ISyncVar);

            var keyStyle = new GUIStyle();
            keyStyle.normal.textColor = Color.cyan;
            keyStyle.fontSize = 13;

            var valueStyle = new GUIStyle();
            valueStyle.normal.textColor = Color.white;
            valueStyle.fontSize = 13;

            var r = EditorGUILayout.BeginVertical();

            r = new Rect(r.x - 13, r.y, EditorGUIUtility.currentViewWidth - 26, r.height);
            EditorGUI.DrawRect(r, Color.black);

            foreach (var field in fieldInfos)
            {
                if (baseInterface.IsAssignableFrom(field.FieldType))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(field.Name, keyStyle);
                    EditorGUILayout.LabelField(field.GetValue(target).ToString(), valueStyle);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        base.OnInspectorGUI();
    }
}
