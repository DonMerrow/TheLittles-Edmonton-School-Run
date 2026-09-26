using UnityEditor;
using UnityEngine;

/// <summary>Quick visual-quality switches. They do not alter scene geometry or gameplay.</summary>
public static class LittlesPreviewTools
{
    [MenuItem("The Littles/Preview/High Quality Preview")]
    private static void HighQualityPreview()
    {
        QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
        QualitySettings.antiAliasing = 4;
        QualitySettings.shadowDistance = 80f;
        Application.targetFrameRate = 60;
        SetCameraQuality(true);
        Debug.Log("The Littles: high-quality preview enabled. Use Full HD in the Game view for a clean check.");
    }

    [MenuItem("The Littles/Preview/Fast Development Mode")]
    private static void FastDevelopmentMode()
    {
        int medium = Mathf.Clamp(2, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(medium, true);
        QualitySettings.antiAliasing = 0;
        QualitySettings.shadowDistance = 35f;
        Application.targetFrameRate = -1;
        SetCameraQuality(false);
        Debug.Log("The Littles: fast development mode enabled.");
    }

    private static void SetCameraQuality(bool high)
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            camera.allowHDR = high;
            camera.allowMSAA = high;
        }
        SceneView.RepaintAll();
    }
}
