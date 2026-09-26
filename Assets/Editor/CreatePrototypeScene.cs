using TheLittles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreatePrototypeScene
{
    [MenuItem("The Littles/Create Prototype Scene")]
    public static void Create()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Prototype Bootstrap").AddComponent<PrototypeBootstrap>();

        const string folder = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, folder + "/Prototype.unity");
        Debug.Log("Created Assets/Scenes/Prototype.unity. Press Play.");
    }
}
