// Assets/Editor/StimIndexMappingWindow.cs
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class StimIndexMappingWindow : EditorWindow
{
    private string acr = "XYZ";
    private string inputFolder = "Assets/Resources/Stimuli";
    private string outputFolder = "Assets/Resources/Configs";
    private string outputFilename = "Stim_Index.csv";

    [MenuItem("Tools/Stim Index Mapping")]
    public static void ShowWindow() => GetWindow<StimIndexMappingWindow>("Stim Index Mapping");

    void OnGUI()
    {
        acr = EditorGUILayout.TextField("Acronym (Like XYZ):", acr);
        GUILayout.Label("Generate Stim‑Index CSV", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Input folder field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Input FBX Folder");
        inputFolder = EditorGUILayout.TextField(inputFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string sel = EditorUtility.OpenFolderPanel("Select FBX Folder", inputFolder, "");
            if (!string.IsNullOrEmpty(sel) && sel.StartsWith(Application.dataPath))
                inputFolder = "Assets" + sel.Substring(Application.dataPath.Length);
        }
        EditorGUILayout.EndHorizontal();

        // Output folder field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Output Config Folder");
        outputFolder = $"Assets/Resources/Configs/{acr}";
        outputFolder = EditorGUILayout.TextField(outputFolder);
        
        EditorGUILayout.EndHorizontal();

        outputFilename = $"{acr}_Stim_Index.csv";
        outputFilename = EditorGUILayout.TextField("Output Filename", outputFilename);
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate CSV"))
            GenerateStimIndexCSV();
    }

    private void GenerateStimIndexCSV()
    {
        if (!Directory.Exists(inputFolder))
        {
            EditorUtility.DisplayDialog("Error", $"Input folder not found: {inputFolder}", "OK");
            return;
        }

        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        

        var fbxFiles = Directory.GetFiles(inputFolder, "*.fbx", SearchOption.TopDirectoryOnly)
                                .Select(Path.GetFileNameWithoutExtension)
                                .OrderBy(n => n)
                                .ToArray();

        string path = Path.Combine(outputFolder, $"{acr}_Stim_Index.csv");
        using (var writer = new StreamWriter(path, false))
        {
            writer.WriteLine("StimIndex,FileName");
            for (int i = 0; i < fbxFiles.Length; i++)
                writer.WriteLine($"{i},{fbxFiles[i]}");
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"Wrote {fbxFiles.Length} entries to {path}", "OK");
    }
}
