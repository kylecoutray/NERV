// Assets/Editor/BuildMirrorToStreamingAssets.cs
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

class BuildMirrorToStreamingAssets : IPreprocessBuildWithReport {
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report) {
        string saRoot = Path.Combine(Application.dataPath, "StreamingAssets");

        // ensure StreamingAssets exists
        if (!Directory.Exists(saRoot))
            Directory.CreateDirectory(saRoot);

        // 1) Mirror only .cs from Scripts/Tasks → StreamingAssets/Scripts/Tasks
        MirrorFolder(
            srcRelative: "Scripts/Tasks",
            dstRelativeInSA: "Scripts/Tasks",
            saRoot: saRoot,
            searchPattern: "*.cs"
        );

        // 2) Mirror only .csv from Resources/Configs → StreamingAssets/Configs
        MirrorFolder(
            srcRelative: "Resources/Configs",
            dstRelativeInSA: "Configs",
            saRoot: saRoot,
            searchPattern: "*.csv"
        );

        AssetDatabase.Refresh();
        Debug.Log("[BuildMirror] Synced .cs and .csv → StreamingAssets");
    }

    private void MirrorFolder(string srcRelative, string dstRelativeInSA, string saRoot, string searchPattern) {
        string srcRoot = Path.Combine(Application.dataPath, srcRelative);
        string dstRoot = Path.Combine(saRoot, dstRelativeInSA);

        if (!Directory.Exists(srcRoot)) {
            Debug.LogWarning($"[BuildMirror] Source folder not found: {srcRoot}");
            return;
        }

        // remove any old copy
        if (Directory.Exists(dstRoot))
            Directory.Delete(dstRoot, recursive: true);

        // copy matching files
        foreach (var srcFile in Directory.GetFiles(srcRoot, searchPattern, SearchOption.AllDirectories)) {
            string relativePath = srcFile.Substring(srcRoot.Length + 1);
            string dstFile = Path.Combine(dstRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(dstFile));
            File.Copy(srcFile, dstFile, overwrite: true);
        }
    }
}