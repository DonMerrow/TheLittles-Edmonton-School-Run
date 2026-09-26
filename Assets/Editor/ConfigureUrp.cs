using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class ConfigureUrp
{
    private const string SettingsFolder = "Assets/Settings";
    private const string RendererPath = SettingsFolder + "/TheLittlesRenderer.asset";
    private const string PipelinePath = SettingsFolder + "/TheLittlesURP.asset";

    static ConfigureUrp()
    {
        EditorApplication.delayCall += EnsureUrp;
    }

    [MenuItem("The Littles/Configure URP")]
    public static void EnsureUrp()
    {
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset) return;
        if (!AssetDatabase.IsValidFolder(SettingsFolder))
            AssetDatabase.CreateFolder("Assets", "Settings");

        UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }

        UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline == null)
        {
            pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
            SerializedObject serialized = new(pipeline);
            SerializedProperty renderers = serialized.FindProperty("m_RendererDataList");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            SerializedProperty defaultRenderer = serialized.FindProperty("m_DefaultRendererIndex");
            if (defaultRenderer != null) defaultRenderer.intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();
        Debug.Log("The Littles now uses URP. No cloud connection is involved.");
    }
}
