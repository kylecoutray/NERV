using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

[CustomEditor(typeof(ExperimentDefinition))]
public class ExperimentDefinitionEditor : Editor
{
    private SerializedProperty _statesProp;
    private SerializedProperty _acronymProp;

    private void OnEnable()
    {
        _statesProp  = serializedObject.FindProperty("States");
        _acronymProp = serializedObject.FindProperty("Acronym");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw the acronym field
        EditorGUILayout.PropertyField(_acronymProp);

        EditorGUILayout.Space();

        // Draw and manipulate the States list
        EditorGUILayout.LabelField("States", EditorStyles.boldLabel);

        for (int i = 0; i < _statesProp.arraySize; i++)
        {
            var stateProp = _statesProp.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical("box");

            // Draw all base fields
            EditorGUILayout.PropertyField(stateProp.FindPropertyRelative("Name"));
            EditorGUILayout.PropertyField(stateProp.FindPropertyRelative("IsTTL"));
            
            // Draw IsStimulus and, if true, show IsSample
            var stimProp  = stateProp.FindPropertyRelative("IsStimulus");
            EditorGUILayout.PropertyField(stimProp);
            if (stimProp.boolValue)
            {
                EditorGUILayout.PropertyField(stateProp.FindPropertyRelative("IsSample"));
            }

            // Draw remaining flags
            EditorGUILayout.PropertyField(stateProp.FindPropertyRelative("IsDelay"));
            EditorGUILayout.PropertyField(stateProp.FindPropertyRelative("IsChoice"));
            EditorGUILayout.PropertyField(stateProp.FindPropertyRelative("IsFeedback"));
            EditorGUILayout.PropertyField(stateProp.FindPropertyRelative("IsClearAll"));

            // Draw other data fields
            EditorGUILayout.PropertyField(stateProp.FindPropertyRelative("TTLCode"));
            EditorGUILayout.PropertyField(stateProp.FindPropertyRelative("PostStateDelay"));

            if (GUILayout.Button("Remove State"))
            {
                Undo.RecordObject(target, "Remove State");
                _statesProp.DeleteArrayElementAtIndex(i);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        // “New State” button
        if (GUILayout.Button("New State", GUILayout.Height(30)))
        {
            Undo.RecordObject(target, "Add New State");
            _statesProp.arraySize++;
            serializedObject.ApplyModifiedProperties();

            // Grab the newly added element
            var newElem = _statesProp.GetArrayElementAtIndex(_statesProp.arraySize - 1);

            // Set defaults
            newElem.FindPropertyRelative("Name").stringValue        = "NewState";
            newElem.FindPropertyRelative("IsTTL").boolValue         = false;
            newElem.FindPropertyRelative("IsStimulus").boolValue    = false;
            newElem.FindPropertyRelative("IsSample").boolValue      = false;
            newElem.FindPropertyRelative("IsDelay").boolValue       = false;
            newElem.FindPropertyRelative("IsChoice").boolValue      = false;
            newElem.FindPropertyRelative("IsFeedback").boolValue    = false;
            newElem.FindPropertyRelative("IsClearAll").boolValue    = false;
            newElem.FindPropertyRelative("TTLCode").intValue        = 0;
            newElem.FindPropertyRelative("PostStateDelay").floatValue = 0f;

            // Commit the changes
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
