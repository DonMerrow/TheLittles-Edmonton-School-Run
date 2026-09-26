using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Extracts the clips used by the level into runtime-loadable assets.</summary>
public static class QuaterniusAnimalAssetSetup
{
    private const string Root = "Assets/Resources/Littles/RiverValley/Animals/Quaternius";

    [MenuItem("Tools/The Littles/Prepare Quaternius Animals")]
    public static void Prepare()
    {
        Extract("Wolf", "Walk");
        Extract("Husky", "Walk");
        Extract("Deer", "Walk");
        Extract("Fox", "Walk");
        Extract("Stag", "Walk");
        Extract("ShibaInu", "Walk");
        Extract("Bunny", "Walk");
        AssetDatabase.SaveAssets();
    }

    private static void Extract(string modelName, string wantedClip)
    {
        string modelPath = $"{Root}/{modelName}.fbx";
        AnimationClip source = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<AnimationClip>()
            .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(clip => string.Equals(clip.name, wantedClip, StringComparison.OrdinalIgnoreCase) ||
                clip.name.EndsWith("|" + wantedClip, StringComparison.OrdinalIgnoreCase) ||
                clip.name.EndsWith("_" + wantedClip, StringComparison.OrdinalIgnoreCase));
        if (source == null)
        {
            Debug.LogError($"No '{wantedClip}' animation was found inside {modelPath}.");
            return;
        }

        string outputPath = $"{Root}/{modelName}_{wantedClip}.anim";
        AnimationClip output = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
        if (output == null)
        {
            output = new AnimationClip();
            AssetDatabase.CreateAsset(output, outputPath);
        }
        EditorUtility.CopySerialized(source, output);
        output.name = $"{modelName}_{wantedClip}";
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(output);
        settings.loopTime = true;
        settings.loopBlend = true;
        AnimationUtility.SetAnimationClipSettings(output, settings);
        EditorUtility.SetDirty(output);
        Debug.Log($"Prepared animated animal clip: {output.name} ({source.length:0.00}s).", output);
    }
}
