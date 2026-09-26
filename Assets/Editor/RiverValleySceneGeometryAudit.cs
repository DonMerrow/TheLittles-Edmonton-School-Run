using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RiverValleySceneGeometryAudit
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/EdmontonRiverValleySchoolRun.unity");
        using StreamWriter report=new("/tmp/thelittles-geometry-audit.txt");
        const string rabbitPath="Assets/Resources/Littles/RiverValley/Animals/FoundRabbit/Rabbit.fbx";
        foreach(Object asset in AssetDatabase.LoadAllAssetsAtPath(rabbitPath))
            if(asset is AnimationClip clip)report.WriteLine($"Rabbit animation: {clip.name}, {clip.length:F2}s");
        Transform rabbit=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .FirstOrDefault(item=>item.name=="Found rabbit animated model");
        if(rabbit!=null)
        {
            Renderer[] parts=rabbit.GetComponentsInChildren<Renderer>(true);
            if(parts.Length>0)
            {
                Bounds rabbitBounds=parts[0].bounds;
                for(int i=1;i<parts.Length;i++)rabbitBounds.Encapsulate(parts[i].bounds);
                report.WriteLine($"Rabbit visual: {rabbitBounds.min:F2}..{rabbitBounds.max:F2}; materials {string.Join(",",parts.SelectMany(p=>p.sharedMaterials).Where(m=>m!=null).Select(m=>m.name).Distinct())}");
                report.WriteLine($"Rabbit texture slots: {string.Join(",",parts.SelectMany(p=>p.sharedMaterials).Where(m=>m!=null).Select(m=>m.name+"="+(m.mainTexture!=null?m.mainTexture.name:"none")).Distinct())}");
            }
        }
        foreach(Transform item in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(item=>item.name.Contains("home")||item.name.Contains("street light")||item.name.Contains("sign")))
        {
            if(item.parent!=null&&(item.parent.name.Contains("home")||item.parent.name.Contains("street light")))continue;
            Renderer[] renderers=item.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)continue;
            Bounds bounds=renderers[0].bounds;
            for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
            bool nearRoute=bounds.max.x>-4f&&bounds.min.x<5f&&bounds.max.z>-10f&&bounds.min.z<138f;
            if(nearRoute||item.name.Contains("home")||item.name.Contains("street light"))
                report.WriteLine($"{item.name}: position={item.position:F1}; bounds={bounds.min:F1}..{bounds.max:F1}; routeOverlap={nearRoute}");
        }
        EditorApplication.Exit(0);
    }
}
