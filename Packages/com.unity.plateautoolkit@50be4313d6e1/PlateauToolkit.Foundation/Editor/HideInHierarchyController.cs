using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

// Make sure your custom editor targets the correct type
[CustomEditor(typeof(HideInHierarchy))]
public class HideInHierarchyController : Editor
{
    public override void OnInspectorGUI()
    {
        HideInHierarchy myScript = (HideInHierarchy)target;

        EditorGUI.BeginChangeCheck();
        bool checkboxValue = EditorGUILayout.Toggle("Hide child objects", myScript.m_ToggleHideChildren);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(myScript, "Toggle to hide children objects");
            myScript.m_ToggleHideChildren = checkboxValue;
        }
    }
}