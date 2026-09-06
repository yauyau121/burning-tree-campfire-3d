using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class OmniUnityBuild
{
    public static void PerformBuild()
    {
        string[] args = Environment.GetCommandLineArgs();
        string targetPlatformStr = GetCustomArg(args, "-customBuildTarget") ?? "StandaloneWindows64";
        string targetPath = GetCustomArg(args, "-customBuildPath");

        BuildTarget target;
        if (!Enum.TryParse(targetPlatformStr, true, out target))
        {
            Debug.LogError("[OmniUnity Build] Unknown BuildTarget: " + targetPlatformStr);
            EditorApplication.Exit(1);
            return;
        }

        if (string.IsNullOrEmpty(targetPath))
        {
            string extension = target switch
            {
                BuildTarget.StandaloneWindows64 => ".exe",
                BuildTarget.StandaloneWindows => ".exe",
                BuildTarget.StandaloneLinux64 => ".x86_64",
                BuildTarget.Android => ".apk",
                BuildTarget.StandaloneOSX => ".app",
                _ => ""
            };
            targetPath = Path.Combine("Build", targetPlatformStr, "App" + extension);
        }

        string outputDir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            scenes = new[] { "Assets/Scenes/MainScene.unity" };
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = targetPath,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        Debug.Log($"[OmniUnity Build] Target: {target}, Path: {targetPath}, Result: {summary.result}, Size: {summary.totalSize} bytes");

        if (summary.result == BuildResult.Succeeded)
        {
            EditorApplication.Exit(0);
        }
        else
        {
            EditorApplication.Exit(1);
        }
    }

    private static string GetCustomArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
