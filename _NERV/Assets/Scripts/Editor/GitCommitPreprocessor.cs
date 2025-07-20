#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Diagnostics;
using System.IO;

class GitCommitPreprocessor : IPreprocessBuildWithReport
{
    // Lower numbers run earlier if you have multiple preprocessors
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        // 1. Get the hash
        string hash = GetGitHash();

        // 2. Ensure the Resources folder exists
        string resPath = "Assets/Resources";
        if (!Directory.Exists(resPath))
            Directory.CreateDirectory(resPath);

        // 3. Write to git_commit.txt
        File.WriteAllText(Path.Combine(resPath, "git_commit.txt"), hash);

        // 4. Tell Unity to pick up the new file
        AssetDatabase.Refresh();

        UnityEngine.Debug.Log($"[BuildPreprocessor] Wrote Git commit hash: {hash}");
    }

    private string GetGitHash()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse HEAD",
                WorkingDirectory = Application.dataPath + "/..",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                    return output;

                // If git fails (not a repo, etc.)
                UnityEngine.Debug.LogWarning("[BuildPreprocessor] Git returned no commit. Using last recorded commit.");
            }
        }
        catch
        {
            UnityEngine.Debug.LogWarning("[BuildPreprocessor] Git is not installed. Using last recorded commit.");
        }

        // If Git failed, fall back to the last saved commit (if any)
        string commitPath = Path.Combine(Application.dataPath, "Resources/git_commit.txt");
        if (File.Exists(commitPath))
            return File.ReadAllText(commitPath).Trim();

        return "unknown"; // Nothing to fall back to
    }
}
#endif
