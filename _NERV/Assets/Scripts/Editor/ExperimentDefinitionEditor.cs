// Place this script in Assets/Scripts/Editor/
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(ExperimentDefinition))]
public class ExperimentDefinitionEditor : Editor
{
    private ExperimentDefinition _def;

    private void OnEnable()
    {
        _def = (ExperimentDefinition)target;
    }

    public override void OnInspectorGUI()
    {
        // Draw default fields for Acronym, States list, etc.
        DrawDefaultInspector();
        EditorGUILayout.Space();

        // Custom "New State" button
        if (GUILayout.Button("New State", GUILayout.Height(30)))
        {
            // Support undo
            Undo.RecordObject(_def, "Add New State");

            // Ensure list exists
            if (_def.States == null)
                _def.States = new List<StateDefinition>();

            // Append fresh default state
            _def.States.Add(new StateDefinition());

            // Mark asset dirty so changes are saved
            EditorUtility.SetDirty(_def);
        }
    }
}
