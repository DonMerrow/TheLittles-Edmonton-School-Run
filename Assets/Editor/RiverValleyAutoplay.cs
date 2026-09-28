using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class RiverValleyAutoplay
{
    private const string ScenePath = "Assets/Scenes/EdmontonRiverValleySchoolRun.unity";
    private const string BuildDirectory = "/tmp/TheLittlesAutoplay";
    private const string ExecutablePath = BuildDirectory + "/TheLittlesAutoplay.x86_64";

    public static void BuildReleaseCandidate()
    {
        BuildTo("/tmp/TheLittlesReleaseQA/TheLittles.x86_64");
    }

    public static void BuildStandalone()
    {
        BuildTo(ExecutablePath);
    }

    public static void BuildAndroidRelease()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
        string apkPath = Path.Combine(projectRoot,"Builds","TheLittles-Android","TheLittles.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(apkPath));
        PlayerSettings.companyName = "The Littles";
        PlayerSettings.productName = "The Littles: Edmonton River Valley School Run";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "ca.thelittles.rivervalleyschoolrun");
        PlayerSettings.bundleVersion = "1.0.3";
        PlayerSettings.Android.bundleVersionCode = 4;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        EditorUserBuildSettings.buildAppBundle = false;
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Android build failed: " + report.summary.result);
        Debug.Log($"ANDROID BUILD COMPLETE: {apkPath} ({report.summary.totalSize} bytes)");
        EditorApplication.Exit(0);
    }

    private static void BuildTo(string executablePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(executablePath));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = executablePath,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Autoplay build failed: " + report.summary.result);
        Debug.Log($"STANDALONE BUILD COMPLETE: {executablePath} ({report.summary.totalSize} bytes)");
        EditorApplication.Exit(0);
    }
}
