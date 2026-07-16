using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

/// Xuất bản game thành file .exe Windows standalone để nộp bài, không cần mở Unity.
public static class BuildTools
{
    [MenuItem("ShatteredGate/Build Windows Executable (.exe)")]
    static void BuildWindows()
    {
        const string scenePath = "Assets/Scenes/Main.unity";
        if (!File.Exists(scenePath))
        {
            Debug.LogError("[BuildTools] Không tìm thấy Assets/Scenes/Main.unity — chạy 'Build Vertical Slice Scene' trước.");
            return;
        }

        string buildFolder = "Builds/Windows";
        Directory.CreateDirectory(buildFolder);
        string exePath = $"{buildFolder}/ShatteredGate.exe";

        var options = new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildTools] Build thành công! Tổng dung lượng: {report.summary.totalSize / (1024 * 1024)} MB. " +
                      $"File tại: {Path.GetFullPath(buildFolder)}");
            EditorUtility.RevealInFinder(exePath);
        }
        else
        {
            Debug.LogError($"[BuildTools] Build thất bại: {report.summary.result}. Xem chi tiết ở các dòng log phía trên.");
        }
    }
}
